// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class StructuralJsonTypeClassifierTests(JsonSerializerWrapper serializerUnderTest) : SerializerTests(serializerUnderTest)
    {
        private const string LongPropertyName =
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" +
            "a";

        private readonly JsonSerializerOptions _options = new(serializerUnderTest.DefaultOptions);
        private readonly JsonSerializerOptions _caseInsensitiveOptions = new(serializerUnderTest.DefaultOptions)
        {
            PropertyNameCaseInsensitive = true
        };
        private readonly JsonSerializerOptions _disallowUnmappedOptions = new(serializerUnderTest.DefaultOptions)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        private readonly JsonSerializerOptions _numberFromStringOptions = new(serializerUnderTest.DefaultOptions)
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        [Fact]
        public async Task StructuralClassifier_DistinguishesObjectProperties()
        {
            PetUnion? dog = await Serializer.DeserializeWrapper<PetUnion>("""{"Name":"Rex","Breed":"Labrador"}""", _options);
            Assert.NotNull(dog);
            Dog dogValue = Assert.IsType<Dog>(GetUnionValue(dog));
            Assert.Equal("Rex", dogValue.Name);
            Assert.Equal("Labrador", dogValue.Breed);

            PetUnion? cat = await Serializer.DeserializeWrapper<PetUnion>("""{"Name":"Misty","Lives":9}""", _options);
            Assert.NotNull(cat);
            Cat catValue = Assert.IsType<Cat>(GetUnionValue(cat));
            Assert.Equal("Misty", catValue.Name);
            Assert.Equal(9, catValue.Lives);
        }

        [Theory]
        [InlineData("42", typeof(int))]
        [InlineData("\"text\"", typeof(string))]
        [InlineData("true", typeof(bool))]
        [InlineData("[1,2,3]", typeof(List<int>))]
        [InlineData("""{"Name":"Rex","Breed":"Labrador"}""", typeof(Dog))]
        public async Task StructuralClassifier_DistinguishesJsonValueTypes(string json, Type expectedType)
        {
            UniqueShapeUnion? result = await Serializer.DeserializeWrapper<UniqueShapeUnion>(json, _options);
            Assert.NotNull(result);
            Assert.IsType(expectedType, GetUnionValue(result));
        }

        [Fact]
        public async Task StructuralClassifier_DistinguishesArrayFromDictionary()
        {
            ArrayOrDictionaryUnion? array = await Serializer.DeserializeWrapper<ArrayOrDictionaryUnion>("[1,2,3]", _options);
            Assert.NotNull(array);
            Assert.Equal([1, 2, 3], Assert.IsType<List<int>>(GetUnionValue(array)));

            ArrayOrDictionaryUnion? dictionary = await Serializer.DeserializeWrapper<ArrayOrDictionaryUnion>("""{"one":1,"two":2}""", _options);
            Assert.NotNull(dictionary);
            Assert.Equal(2, Assert.IsType<Dictionary<string, int>>(GetUnionValue(dictionary))["two"]);
        }

        [Fact]
        public async Task StructuralClassifier_SupportsBuiltInNodeCaseTypes()
        {
            JsonArrayOrStringUnion? array =
                await Serializer.DeserializeWrapper<JsonArrayOrStringUnion>("[1,2,3]", _options);
            Assert.NotNull(array);
            Assert.Equal(3, Assert.IsType<JsonArray>(GetUnionValue(array)).Count);

            JsonObjectOrStringUnion? objectValue =
                await Serializer.DeserializeWrapper<JsonObjectOrStringUnion>("""{"Value":42}""", _options);
            Assert.NotNull(objectValue);
            Assert.Equal(42, Assert.IsType<JsonObject>(GetUnionValue(objectValue))["Value"]!.GetValue<int>());
        }

        [Fact]
        public async Task StructuralClassifier_DoesNotSupportPreserveReferences()
        {
            JsonSerializerOptions options = new(_options)
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };

            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper<PetUnion>(
                    """{"Name":"Rex","Breed":"Labrador"}""",
                    options));

            Assert.Contains(
                "Reference-preserving deserialization is not supported.",
                exception.Message);
        }

        [Fact]
        public async Task StructuralClassifier_UsesJsonTypeInfoPropertyNames()
        {
            RenamedPropertyUnion? result = await Serializer.DeserializeWrapper<RenamedPropertyUnion>("""{"kind":"special"}""", _options);
            Assert.NotNull(result);
            RenamedPropertyCase value = Assert.IsType<RenamedPropertyCase>(GetUnionValue(result));
            Assert.Equal("special", value.Kind);
        }

        [Fact]
        public async Task StructuralClassifier_UsesCaseInsensitivePropertyNames()
        {
            PetUnion? dog = await Serializer.DeserializeWrapper<PetUnion>("""{"\u006eAME":"Rex","breed":"Labrador"}""", _caseInsensitiveOptions);
            Assert.NotNull(dog);
            Dog dogValue = Assert.IsType<Dog>(GetUnionValue(dog));
            Assert.Equal("Rex", dogValue.Name);
            Assert.Equal("Labrador", dogValue.Breed);

            UnicodePropertyUnion? unicode = await Serializer.DeserializeWrapper<UnicodePropertyUnion>("""{"\u00E5ngstr\u00F6m":1}""", _caseInsensitiveOptions);
            Assert.NotNull(unicode);
            Assert.Equal(1, Assert.IsType<UnicodePropertyCase>(GetUnionValue(unicode)).Value);
        }

        [Fact]
        public void StructuralClassifier_SegmentedEscapedPropertyNameIsClassified()
        {
            byte[] json = System.Text.Encoding.UTF8.GetBytes(
                """{"\u006eAME":"Rex","breed":"Labrador"}""");
            ByteSequenceSegment firstSegment = new(json.AsMemory(0, 6));
            ByteSequenceSegment lastSegment = firstSegment.Append(json.AsMemory(6, 6));
            lastSegment = lastSegment.Append(json.AsMemory(12));
            ReadOnlySequence<byte> sequence = new(
                firstSegment,
                0,
                lastSegment,
                lastSegment.Memory.Length);
            Utf8JsonReader reader = new(sequence);
            JsonTypeInfo<PetUnion> typeInfo =
                Serializer.GetTypeInfo<PetUnion>(_caseInsensitiveOptions);

            PetUnion? result = JsonSerializer.Deserialize(ref reader, typeInfo);

            Assert.True(result.HasValue);
            Dog dog = Assert.IsType<Dog>(result.GetValueOrDefault().Value);
            Assert.Equal("Rex", dog.Name);
            Assert.Equal("Labrador", dog.Breed);
        }

        [Fact]
        public async Task StructuralClassifier_LongEscapedPropertyNameIsClassified()
        {
            string escapedPropertyName = "\\u0061" + LongPropertyName.Substring(1);
            string json = "{\"" + escapedPropertyName + "\":\"Labrador\"}";

            LongPropertyUnion? result =
                await Serializer.DeserializeWrapper<LongPropertyUnion>(json, _options);

            Assert.NotNull(result);
            Assert.Equal("Labrador", Assert.IsType<LongPropertyCase>(GetUnionValue(result)).Value);
        }

        [Fact]
        public async Task StructuralClassifier_UsesNumberHandlingMetadataWithoutInspectingStringContent()
        {
            NumberHandlingUnion? fromString = await Serializer.DeserializeWrapper<NumberHandlingUnion>("\"42\"", _numberFromStringOptions);
            Assert.NotNull(fromString);
            Assert.Equal(42, Assert.IsType<int>(GetUnionValue(fromString)));

            NumberHandlingUnion? fromNumber = await Serializer.DeserializeWrapper<NumberHandlingUnion>("42", _numberFromStringOptions);
            Assert.NotNull(fromNumber);
            Assert.Equal(42, Assert.IsType<int>(GetUnionValue(fromNumber)));

            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<NumberHandlingUnion>("\"not-a-number\"", _numberFromStringOptions));
        }

        [Fact]
        public async Task StructuralClassifier_CustomNullableConverterIsNotUnwrapped()
        {
            JsonSerializerOptions options = new(_options);
            options.Converters.Add(new NullableInt32AsStringConverter());

            await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper<CustomNullableConverterUnion>("\"42\"", options));
        }

        [Theory]
        [InlineData(typeof(PolymorphicOrStringUnion), nameof(PolyAnimal))]
        [InlineData(typeof(PolymorphicCollectionOrStringUnion), nameof(PolymorphicIntList))]
        [InlineData(typeof(CaseSensitiveDiscriminatorUnion), nameof(LowercaseDiscriminatorBase))]
        public async Task StructuralClassifier_DoesNotSupportPolymorphicCaseTypes(
            Type unionType,
            string caseTypeName)
        {
            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper("{}", unionType, _options));

            Assert.Contains(
                "Union cases that use polymorphism or are union types are not supported.",
                exception.Message);
            Assert.Contains(caseTypeName, exception.Message);
        }

        [Fact]
        public async Task StructuralClassifier_DerivedFactoryCannotClassifyPolymorphicTypes()
        {
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Serializer.DeserializeWrapper<MisconfiguredPolymorphicBase>(
                    """{"$type":"derived"}""",
                    _options));

            Assert.Contains("only supports union types", exception.Message);
        }

        [Fact]
        public async Task StructuralClassifier_ExtensionDataOverridesGlobalDisallowUnmappedMembers()
        {
            ExtensionDataUnion? result = await Serializer.DeserializeWrapper<ExtensionDataUnion>(
                """{"Id":42,"Additional":"value"}""",
                _disallowUnmappedOptions);

            Assert.NotNull(result);
            ExtensionDataCase extensionDataCase = Assert.IsType<ExtensionDataCase>(GetUnionValue(result));
            Assert.Equal(42, extensionDataCase.Id);
            Assert.Equal("value", extensionDataCase.ExtensionData["Additional"].GetString());

            result = await Serializer.DeserializeWrapper<ExtensionDataUnion>(
                """{"Id":42,"Known":"value"}""",
                _disallowUnmappedOptions);

            Assert.NotNull(result);
            Assert.Equal("value", Assert.IsType<ExtensionDataFallback>(GetUnionValue(result)).Known);
        }

        [Theory]
        [InlineData("""{"Name":"Shared"}""")]
        [InlineData("{}")]
        [InlineData("true")]
        public async Task StructuralClassifier_AmbiguousOrUnsupportedPayloadThrows(string json)
        {
            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<PetUnion>(json, _options));
        }

        [Theory]
        [InlineData("{\"Breed\":")]
        [InlineData("{\"Breed\":[")]
        [InlineData("{\"Breed\":\"Labrador\"")]
        public async Task StructuralClassifier_IncompletePayloadThrowsJsonException(string json)
        {
            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<PetUnion>(json, _options));
        }

        [Theory]
        [InlineData("{\"Breed\":")]
        [InlineData("{\"Breed\":[")]
        [InlineData("{\"Breed\":\"Labrador\"")]
        public void StructuralClassifier_NonFinalPayloadThrowsJsonException(string json)
        {
            JsonTypeInfo<PetUnion> typeInfo = Serializer.GetTypeInfo<PetUnion>(_options);
            Assert.ThrowsAny<JsonException>(() => Deserialize(json, typeInfo));

            static void Deserialize(string json, JsonTypeInfo<PetUnion> typeInfo)
            {
                Utf8JsonReader reader = new(
                    System.Text.Encoding.UTF8.GetBytes(json),
                    isFinalBlock: false,
                    state: default);
                JsonSerializer.Deserialize(ref reader, typeInfo);
            }
        }

        [Theory]
        [InlineData("\"text\"", typeof(StringUnion))]
        [InlineData("42", typeof(NumericUnion))]
        [InlineData("true", typeof(BooleanUnion))]
        [InlineData("[]", typeof(ListUnion))]
        public async Task StructuralClassifier_RejectsAmbiguousNonObjectValueTypes(string json, Type unionType)
        {
            await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper(json, unionType, _options));
        }

        [Fact]
        public async Task StructuralClassifier_NumberHandlingCanIntroduceStringAmbiguity()
        {
            await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper<NumericStringUnion>("\"42\"", _numberFromStringOptions));
        }

        [Fact]
        public async Task StructuralClassifier_DoesNotRecursivelyClassifyNestedUnions()
        {
            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper<OuterNestedUnion>("42", _options));

            Assert.Contains(
                "Union cases that use polymorphism or are union types are not supported.",
                exception.Message);
            Assert.Contains(nameof(InnerScalarUnion), exception.Message);
        }

        [Theory]
        [InlineData("""{"one":1}""", typeof(DictionaryUnion))]
        [InlineData("""{"Source":"sensor","Items":[{"Celsius":21.5}]}""", typeof(BatchUnion))]
        [InlineData("""{"Id":1}""", typeof(OptionalSubsetUnion))]
        public async Task StructuralClassifier_RejectsAmbiguousObjectSchemas(string json, Type unionType)
        {
            await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper(json, unionType, _options));
        }

        [Theory]
        [InlineData(typeof(ObjectOrDictionaryUnion))]
        [InlineData(typeof(JsonElementOrRequiredObjectUnion))]
        [InlineData(typeof(JsonValueOrRequiredObjectUnion))]
        public async Task StructuralClassifier_RejectsMixedPocoAndNonPocoJsonObjectCases(Type unionType)
        {
            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper("{}", unionType, _options));

            Assert.Contains("JSON values of type 'Object'", exception.Message);
        }

        [Fact]
        public async Task StructuralClassifier_RejectsMultipleNonPocoJsonObjectCases()
        {
            NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.DeserializeWrapper<JsonObjectOrDictionaryUnion>(
                    """{"one":1}""",
                    _options));

            Assert.Contains("JSON values of type 'Object'", exception.Message);
        }

        [Theory]
        [InlineData("""{"Sku":"A123","Customer":"Contoso","Quantity":2}""", typeof(Order))]
        [InlineData("""{"Quantity":2}""", typeof(Quote))]
        [InlineData("{}", typeof(Quote))]
        public async Task StructuralClassifier_RequiredPropertiesDisqualifyCandidate(string json, Type expectedType)
        {
            RequiredPropertyUnion? result = await Serializer.DeserializeWrapper<RequiredPropertyUnion>(json, _options);

            Assert.NotNull(result);
            Assert.IsType(expectedType, GetUnionValue(result));
        }

        [Theory]
        [InlineData("""{"Sku":"A123","Quantity":2}""")]
        [InlineData("""{"Sku":"A123","Sku":"B456","Quantity":2}""")]
        public async Task StructuralClassifier_RequiredPropertyEvidenceCannotFallBackToAnotherCase(string json)
        {
            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<RequiredPropertyUnion>(json, _options));
        }

        [Theory]
        [InlineData("""{"Name":"Misty"}""", typeof(IdenticalDog))]
        [InlineData("""{"Age":5}""", typeof(IdenticalCat))]
        public async Task StructuralClassifier_RequiredPropertiesDistinguishIdenticalNameSets(string json, Type expectedType)
        {
            IdenticalPetUnion? result = await Serializer.DeserializeWrapper<IdenticalPetUnion>(json, _options);

            Assert.NotNull(result);
            Assert.IsType(expectedType, GetUnionValue(result));
        }

        [Fact]
        public async Task StructuralClassifier_IdenticalNameSetsRemainAmbiguousWhenAllRequiredPropertiesArePresent()
        {
            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<IdenticalPetUnion>(
                    """{"Name":"Misty","Age":5}""",
                    _options));
        }

        [Fact]
        public async Task StructuralClassifier_UnmappedMemberHandlingDisallowDisqualifiesCandidate()
        {
            UnmappedMemberUnion? loose = await Serializer.DeserializeWrapper<UnmappedMemberUnion>("""{"Id":1,"Extra":"x"}""", _options);
            Assert.NotNull(loose);
            Assert.IsType<Loose>(GetUnionValue(loose));

            loose = await Serializer.DeserializeWrapper<UnmappedMemberUnion>("""{"Id":1,"Extra":"x"}""", _caseInsensitiveOptions);
            Assert.NotNull(loose);
            Assert.IsType<Loose>(GetUnionValue(loose));

            UnmappedMemberUnion? strict = await Serializer.DeserializeWrapper<UnmappedMemberUnion>("""{"Id":1,"Note":2}""", _options);
            Assert.NotNull(strict);
            Assert.IsType<Strict>(GetUnionValue(strict));
        }

        [Theory]
        [InlineData("""{"Common":0,"GroupOne":1,"AlphaOnly":2}""", typeof(SubtractionAlpha))]
        [InlineData("""{"AlphaOnly":2,"GroupOne":1,"Common":0}""", typeof(SubtractionAlpha))]
        [InlineData("""{"Common":0,"GroupOne":1,"BetaOnly":2}""", typeof(SubtractionBeta))]
        [InlineData("""{"Common":0,"GroupTwo":1,"GammaOnly":2}""", typeof(SubtractionGamma))]
        [InlineData("""{"Common":0,"GroupTwo":1,"DeltaOnly":2}""", typeof(SubtractionDelta))]
        public async Task StructuralClassifier_SubtractsCandidatesAcrossMultipleProperties(
            string json,
            Type expectedType)
        {
            SubtractionUnion? result =
                await Serializer.DeserializeWrapper<SubtractionUnion>(json, _options);

            Assert.NotNull(result);
            Assert.IsType(expectedType, GetUnionValue(result));
        }

        [Theory]
        [InlineData("""{"Unknown":0,"GroupOne":1}""", typeof(SubtractionAlpha))]
        [InlineData("""{"GroupOne":1,"Unknown":0}""", typeof(SubtractionAlpha))]
        [InlineData("""{"Unknown":0,"GroupTwo":1}""", typeof(SubtractionGamma))]
        [InlineData("""{"GroupTwo":1,"Unknown":0}""", typeof(SubtractionGamma))]
        public async Task StructuralClassifier_UnknownPropertiesSubtractStrictCandidates(
            string json,
            Type expectedType)
        {
            SubtractionUnion? result =
                await Serializer.DeserializeWrapper<SubtractionUnion>(json, _options);

            Assert.NotNull(result);
            Assert.IsType(expectedType, GetUnionValue(result));
        }

        [Theory]
        [InlineData("""{"Unknown":0,"GroupOne":1,"BetaOnly":2}""")]
        [InlineData("""{"GroupOne":1,"BetaOnly":2,"Unknown":0}""")]
        [InlineData("""{"GroupOne":1}""")]
        [InlineData("""{"GroupTwo":1}""")]
        [InlineData("""{"AlphaOnly":1,"GammaOnly":2}""")]
        public async Task StructuralClassifier_SubtractionCanProduceNoUniqueCandidate(string json)
        {
            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<SubtractionUnion>(json, _options));
        }

        [Theory]
        [InlineData("""{"Name":"Misty","Age":5}""", typeof(RequiredOverlapA))]
        [InlineData("""{"Name":"Misty","Breed":"Siamese"}""", typeof(RequiredOverlapB))]
        public async Task StructuralClassifier_OverlappingRequiredPropertySetsRemainReachable(
            string json,
            Type expectedType)
        {
            RequiredOverlapUnion? result =
                await Serializer.DeserializeWrapper<RequiredOverlapUnion>(json, _options);

            Assert.NotNull(result);
            Assert.IsType(expectedType, GetUnionValue(result));
        }

        [Theory]
        [InlineData("""{"AOnly":1,"B1":4,"B2":5}""")]
        [InlineData("""{"B1":4,"B2":5,"AOnly":1}""")]
        [InlineData("""{"AOnly":1,"AOnly":2,"AOnly":3,"B1":4,"B2":5}""")]
        public async Task StructuralClassifier_ConflictingPropertyEvidenceThrows(string json)
        {
            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<DuplicatePropertyNameUnion>(json, _options));
        }

        [Fact]
        public async Task StructuralClassifier_TracksMoreThan64PropertyNames()
        {
            LargePropertyUnion? result = await Serializer.DeserializeWrapper<LargePropertyUnion>(
                """{"P64":1}""",
                _options);

            Assert.NotNull(result);
            Assert.Equal(1, Assert.IsType<LargePropertyCase>(GetUnionValue(result)).P64);

            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<LargePropertyUnion>(
                    """{"P64":1,"P64":2,"Q":3}""",
                    _options));
        }

        [Fact]
        public async Task StructuralClassifier_SupportsMoreThanEightObjectCases()
        {
            ManyObjectCasesUnion? result = await Serializer.DeserializeWrapper<ManyObjectCasesUnion>(
                """{"Q":42}""",
                _options);

            Assert.NotNull(result);
            Assert.Equal(42, Assert.IsType<SinglePropertyCase>(GetUnionValue(result)).Q);
        }

        [Fact]
        public async Task StructuralClassifier_SupportsMoreThan64ObjectCases()
        {
            LargeObjectCaseUnion? result =
                await Serializer.DeserializeWrapper<LargeObjectCaseUnion>(
                    """{"P64":42}""",
                    _options);

            Assert.NotNull(result);
            Assert.Equal(42, Assert.IsType<ObjectCase64>(GetUnionValue(result)).P64);

            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<LargeObjectCaseUnion>(
                    """{"P00":0,"P64":64}""",
                    _options));
        }

        [Fact]
        public async Task StructuralClassifier_SupportsSelfReferentialCaseTypes()
        {
            TreeUnion? tree = await Serializer.DeserializeWrapper<TreeUnion>("""{"Value":1,"Left":{"Value":2,"Left":null,"Right":null},"Right":null}""", _options);
            Assert.NotNull(tree);
            Assert.IsType<TreeNode>(GetUnionValue(tree));

            TreeUnion? leaf = await Serializer.DeserializeWrapper<TreeUnion>("""{"Value":1}""", _options);
            Assert.NotNull(leaf);
            Assert.IsType<Leaf>(GetUnionValue(leaf));
        }

        private static object? GetUnionValue<TUnion>(TUnion? union)
            where TUnion : struct, IUnion
        {
            Assert.True(union.HasValue);

            return union.GetValueOrDefault().Value;
        }

        private sealed class NullableInt32AsStringConverter : JsonConverter<int?>
        {
            public override int? Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options) =>
                int.Parse(reader.GetString()!, CultureInfo.InvariantCulture);

            public override void Write(
                Utf8JsonWriter writer,
                int? value,
                JsonSerializerOptions options) =>
                writer.WriteStringValue(value?.ToString(CultureInfo.InvariantCulture));
        }

        private sealed class ByteSequenceSegment : ReadOnlySequenceSegment<byte>
        {
            public ByteSequenceSegment(ReadOnlyMemory<byte> memory)
            {
                Memory = memory;
            }

            public ByteSequenceSegment Append(ReadOnlyMemory<byte> memory)
            {
                ByteSequenceSegment segment = new(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };
                Next = segment;
                return segment;
            }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union PetUnion(Dog, Cat);

        public sealed class Dog
        {
            public string? Name { get; set; }
            public string? Breed { get; set; }
        }

        public sealed class Cat
        {
            public string? Name { get; set; }
            public int Lives { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union UniqueShapeUnion(int, string, bool, List<int>, Dog);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union ArrayOrDictionaryUnion(List<int>, Dictionary<string, int>);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union JsonArrayOrStringUnion(JsonArray, string);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union JsonObjectOrDictionaryUnion(JsonObject, Dictionary<string, int>);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union JsonObjectOrStringUnion(JsonObject, string);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union JsonElementOrRequiredObjectUnion(JsonElement, JsonElementRequiredObjectCase);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union JsonValueOrRequiredObjectUnion(JsonValue, JsonElementRequiredObjectCase);

        public sealed class JsonElementRequiredObjectCase
        {
            public required int Required { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union ObjectOrDictionaryUnion(Point, Dictionary<string, int>);

        public sealed class Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union UnicodePropertyUnion(UnicodePropertyCase, UnicodeOtherPropertyCase);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union LongPropertyUnion(LongPropertyCase, UnicodeOtherPropertyCase);

        public sealed class LongPropertyCase
        {
            [JsonPropertyName(LongPropertyName)]
            public string? Value { get; set; }
        }

        public sealed class UnicodePropertyCase
        {
            [JsonPropertyName("\u00C5ngstr\u00F6m")]
            public int Value { get; set; }
        }

        public sealed class UnicodeOtherPropertyCase
        {
            public int Other { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union RenamedPropertyUnion(RenamedPropertyCase, OtherRenamedPropertyCase);

        public sealed class RenamedPropertyCase
        {
            [JsonPropertyName("kind")]
            public string? Kind { get; set; }
        }

        public sealed class OtherRenamedPropertyCase
        {
            public int Code { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union NumberHandlingUnion(int, bool);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union CustomNullableConverterUnion(int?, string);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union NumericStringUnion(int, string);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union StringUnion(Guid, string);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union NumericUnion(int, long);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union BooleanUnion(bool, bool?);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union ListUnion(List<int>, List<string>);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union DictionaryUnion(Dictionary<string, int>, Dictionary<string, string>);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union InnerScalarUnion(int, string);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union OuterNestedUnion(InnerScalarUnion, bool);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union BatchUnion(Batch<TemperatureReading>, Batch<StatusReading>);

        public sealed class Batch<T>
        {
            public string? Source { get; set; }
            public List<T>? Items { get; set; }
        }

        public sealed class TemperatureReading
        {
            public double Celsius { get; set; }
        }

        public sealed class StatusReading
        {
            public bool IsOnline { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union PolymorphicOrStringUnion(PolyAnimal, string);

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
        [JsonDerivedType(typeof(PolyCat), "cat")]
        [JsonDerivedType(typeof(PolyDog), "dog")]
        public record PolyAnimal;

        public sealed record PolyCat(string Name) : PolyAnimal;

        public sealed record PolyDog(string Breed) : PolyAnimal;

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union PolymorphicCollectionOrStringUnion(PolymorphicIntList, string);

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
        [JsonDerivedType(typeof(DerivedPolymorphicIntList), "derived")]
        public class PolymorphicIntList : List<int>
        {
        }

        public sealed class DerivedPolymorphicIntList : PolymorphicIntList
        {
        }

        [JsonPolymorphic(TypeClassifier = typeof(OverlyPermissiveStructuralClassifier))]
        [JsonDerivedType(typeof(MisconfiguredPolymorphicDerived), "derived")]
        public class MisconfiguredPolymorphicBase
        {
        }

        public sealed class MisconfiguredPolymorphicDerived : MisconfiguredPolymorphicBase
        {
        }

        public sealed class OverlyPermissiveStructuralClassifier : JsonUnionTypeStructuralClassifier
        {
            public override bool CanClassify(JsonTypeClassifierContext context) => true;
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union ExtensionDataUnion(ExtensionDataCase, ExtensionDataFallback);

        public sealed class ExtensionDataCase
        {
            public int Id { get; set; }

            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; } = [];
        }

        public sealed class ExtensionDataFallback
        {
            public int Id { get; set; }
            public string? Known { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union CaseSensitiveDiscriminatorUnion(LowercaseDiscriminatorBase, UppercaseDiscriminatorBase);

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
        [JsonDerivedType(typeof(LowercaseDiscriminatorDerived), "lower")]
        public abstract record LowercaseDiscriminatorBase;

        public sealed record LowercaseDiscriminatorDerived : LowercaseDiscriminatorBase;

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "$TYPE")]
        [JsonDerivedType(typeof(UppercaseDiscriminatorDerived), "upper")]
        public abstract record UppercaseDiscriminatorBase;

        public sealed record UppercaseDiscriminatorDerived : UppercaseDiscriminatorBase;

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union IdenticalPetUnion(IdenticalDog, IdenticalCat);

        public sealed class IdenticalDog
        {
            [JsonRequired]
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public sealed class IdenticalCat
        {
            public string? Name { get; set; }

            [JsonRequired]
            public int Age { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union RequiredPropertyUnion(Order, Quote);

        public sealed class Order
        {
            public required string Sku { get; set; }
            public required string Customer { get; set; }
            public int Quantity { get; set; }
        }

        public sealed class Quote
        {
            public int Quantity { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union UnmappedMemberUnion(Strict, Loose);

        [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
        public sealed class Strict
        {
            public int Id { get; set; }
            public int? Note { get; set; }
        }

        public sealed class Loose
        {
            public int Id { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union SubtractionUnion(
            SubtractionAlpha,
            SubtractionBeta,
            SubtractionGamma,
            SubtractionDelta);

        public sealed class SubtractionAlpha
        {
            public int Common { get; set; }
            public int GroupOne { get; set; }
            public int AlphaOnly { get; set; }
        }

        [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
        public sealed class SubtractionBeta
        {
            public int Common { get; set; }
            public int GroupOne { get; set; }
            public int BetaOnly { get; set; }
        }

        public sealed class SubtractionGamma
        {
            public int Common { get; set; }
            public int GroupTwo { get; set; }
            public int GammaOnly { get; set; }
        }

        [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
        public sealed class SubtractionDelta
        {
            public int Common { get; set; }
            public int GroupTwo { get; set; }
            public int DeltaOnly { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union RequiredOverlapUnion(RequiredOverlapA, RequiredOverlapB);

        public sealed class RequiredOverlapA
        {
            [JsonRequired]
            public string? Name { get; set; }

            [JsonRequired]
            public int Age { get; set; }

            public string? Breed { get; set; }
        }

        public sealed class RequiredOverlapB
        {
            [JsonRequired]
            public string? Name { get; set; }

            public int Age { get; set; }

            [JsonRequired]
            public string? Breed { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union DuplicatePropertyNameUnion(DuplicatePropertyA, DuplicatePropertyB);

        public sealed class DuplicatePropertyA
        {
            public int AOnly { get; set; }
        }

        public sealed class DuplicatePropertyB
        {
            public int B1 { get; set; }
            public int B2 { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union LargePropertyUnion(LargePropertyCase, SinglePropertyCase);

        public sealed class LargePropertyCase
        {
            public int P00 { get; set; }
            public int P01 { get; set; }
            public int P02 { get; set; }
            public int P03 { get; set; }
            public int P04 { get; set; }
            public int P05 { get; set; }
            public int P06 { get; set; }
            public int P07 { get; set; }
            public int P08 { get; set; }
            public int P09 { get; set; }
            public int P10 { get; set; }
            public int P11 { get; set; }
            public int P12 { get; set; }
            public int P13 { get; set; }
            public int P14 { get; set; }
            public int P15 { get; set; }
            public int P16 { get; set; }
            public int P17 { get; set; }
            public int P18 { get; set; }
            public int P19 { get; set; }
            public int P20 { get; set; }
            public int P21 { get; set; }
            public int P22 { get; set; }
            public int P23 { get; set; }
            public int P24 { get; set; }
            public int P25 { get; set; }
            public int P26 { get; set; }
            public int P27 { get; set; }
            public int P28 { get; set; }
            public int P29 { get; set; }
            public int P30 { get; set; }
            public int P31 { get; set; }
            public int P32 { get; set; }
            public int P33 { get; set; }
            public int P34 { get; set; }
            public int P35 { get; set; }
            public int P36 { get; set; }
            public int P37 { get; set; }
            public int P38 { get; set; }
            public int P39 { get; set; }
            public int P40 { get; set; }
            public int P41 { get; set; }
            public int P42 { get; set; }
            public int P43 { get; set; }
            public int P44 { get; set; }
            public int P45 { get; set; }
            public int P46 { get; set; }
            public int P47 { get; set; }
            public int P48 { get; set; }
            public int P49 { get; set; }
            public int P50 { get; set; }
            public int P51 { get; set; }
            public int P52 { get; set; }
            public int P53 { get; set; }
            public int P54 { get; set; }
            public int P55 { get; set; }
            public int P56 { get; set; }
            public int P57 { get; set; }
            public int P58 { get; set; }
            public int P59 { get; set; }
            public int P60 { get; set; }
            public int P61 { get; set; }
            public int P62 { get; set; }
            public int P63 { get; set; }

            [JsonRequired]
            public int P64 { get; set; }
        }

        public sealed class SinglePropertyCase
        {
            public int Q { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union OptionalSubsetUnion(OptionalSubsetBase, OptionalSubsetDerived);

        public sealed class OptionalSubsetBase
        {
            public int Id { get; set; }
        }

        public sealed class OptionalSubsetDerived
        {
            public int Id { get; set; }
            public string? Tag { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union ManyObjectCasesUnion(
            Dog,
            Cat,
            Point,
            RenamedPropertyCase,
            OtherRenamedPropertyCase,
            Order,
            Quote,
            Strict,
            SinglePropertyCase);

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union LargeObjectCaseUnion(
            ObjectCase00,
            ObjectCase01,
            ObjectCase02,
            ObjectCase03,
            ObjectCase04,
            ObjectCase05,
            ObjectCase06,
            ObjectCase07,
            ObjectCase08,
            ObjectCase09,
            ObjectCase10,
            ObjectCase11,
            ObjectCase12,
            ObjectCase13,
            ObjectCase14,
            ObjectCase15,
            ObjectCase16,
            ObjectCase17,
            ObjectCase18,
            ObjectCase19,
            ObjectCase20,
            ObjectCase21,
            ObjectCase22,
            ObjectCase23,
            ObjectCase24,
            ObjectCase25,
            ObjectCase26,
            ObjectCase27,
            ObjectCase28,
            ObjectCase29,
            ObjectCase30,
            ObjectCase31,
            ObjectCase32,
            ObjectCase33,
            ObjectCase34,
            ObjectCase35,
            ObjectCase36,
            ObjectCase37,
            ObjectCase38,
            ObjectCase39,
            ObjectCase40,
            ObjectCase41,
            ObjectCase42,
            ObjectCase43,
            ObjectCase44,
            ObjectCase45,
            ObjectCase46,
            ObjectCase47,
            ObjectCase48,
            ObjectCase49,
            ObjectCase50,
            ObjectCase51,
            ObjectCase52,
            ObjectCase53,
            ObjectCase54,
            ObjectCase55,
            ObjectCase56,
            ObjectCase57,
            ObjectCase58,
            ObjectCase59,
            ObjectCase60,
            ObjectCase61,
            ObjectCase62,
            ObjectCase63,
            ObjectCase64);

        public sealed class ObjectCase00 { public int P00 { get; set; } }
        public sealed class ObjectCase01 { public int P01 { get; set; } }
        public sealed class ObjectCase02 { public int P02 { get; set; } }
        public sealed class ObjectCase03 { public int P03 { get; set; } }
        public sealed class ObjectCase04 { public int P04 { get; set; } }
        public sealed class ObjectCase05 { public int P05 { get; set; } }
        public sealed class ObjectCase06 { public int P06 { get; set; } }
        public sealed class ObjectCase07 { public int P07 { get; set; } }
        public sealed class ObjectCase08 { public int P08 { get; set; } }
        public sealed class ObjectCase09 { public int P09 { get; set; } }
        public sealed class ObjectCase10 { public int P10 { get; set; } }
        public sealed class ObjectCase11 { public int P11 { get; set; } }
        public sealed class ObjectCase12 { public int P12 { get; set; } }
        public sealed class ObjectCase13 { public int P13 { get; set; } }
        public sealed class ObjectCase14 { public int P14 { get; set; } }
        public sealed class ObjectCase15 { public int P15 { get; set; } }
        public sealed class ObjectCase16 { public int P16 { get; set; } }
        public sealed class ObjectCase17 { public int P17 { get; set; } }
        public sealed class ObjectCase18 { public int P18 { get; set; } }
        public sealed class ObjectCase19 { public int P19 { get; set; } }
        public sealed class ObjectCase20 { public int P20 { get; set; } }
        public sealed class ObjectCase21 { public int P21 { get; set; } }
        public sealed class ObjectCase22 { public int P22 { get; set; } }
        public sealed class ObjectCase23 { public int P23 { get; set; } }
        public sealed class ObjectCase24 { public int P24 { get; set; } }
        public sealed class ObjectCase25 { public int P25 { get; set; } }
        public sealed class ObjectCase26 { public int P26 { get; set; } }
        public sealed class ObjectCase27 { public int P27 { get; set; } }
        public sealed class ObjectCase28 { public int P28 { get; set; } }
        public sealed class ObjectCase29 { public int P29 { get; set; } }
        public sealed class ObjectCase30 { public int P30 { get; set; } }
        public sealed class ObjectCase31 { public int P31 { get; set; } }
        public sealed class ObjectCase32 { public int P32 { get; set; } }
        public sealed class ObjectCase33 { public int P33 { get; set; } }
        public sealed class ObjectCase34 { public int P34 { get; set; } }
        public sealed class ObjectCase35 { public int P35 { get; set; } }
        public sealed class ObjectCase36 { public int P36 { get; set; } }
        public sealed class ObjectCase37 { public int P37 { get; set; } }
        public sealed class ObjectCase38 { public int P38 { get; set; } }
        public sealed class ObjectCase39 { public int P39 { get; set; } }
        public sealed class ObjectCase40 { public int P40 { get; set; } }
        public sealed class ObjectCase41 { public int P41 { get; set; } }
        public sealed class ObjectCase42 { public int P42 { get; set; } }
        public sealed class ObjectCase43 { public int P43 { get; set; } }
        public sealed class ObjectCase44 { public int P44 { get; set; } }
        public sealed class ObjectCase45 { public int P45 { get; set; } }
        public sealed class ObjectCase46 { public int P46 { get; set; } }
        public sealed class ObjectCase47 { public int P47 { get; set; } }
        public sealed class ObjectCase48 { public int P48 { get; set; } }
        public sealed class ObjectCase49 { public int P49 { get; set; } }
        public sealed class ObjectCase50 { public int P50 { get; set; } }
        public sealed class ObjectCase51 { public int P51 { get; set; } }
        public sealed class ObjectCase52 { public int P52 { get; set; } }
        public sealed class ObjectCase53 { public int P53 { get; set; } }
        public sealed class ObjectCase54 { public int P54 { get; set; } }
        public sealed class ObjectCase55 { public int P55 { get; set; } }
        public sealed class ObjectCase56 { public int P56 { get; set; } }
        public sealed class ObjectCase57 { public int P57 { get; set; } }
        public sealed class ObjectCase58 { public int P58 { get; set; } }
        public sealed class ObjectCase59 { public int P59 { get; set; } }
        public sealed class ObjectCase60 { public int P60 { get; set; } }
        public sealed class ObjectCase61 { public int P61 { get; set; } }
        public sealed class ObjectCase62 { public int P62 { get; set; } }
        public sealed class ObjectCase63 { public int P63 { get; set; } }
        public sealed class ObjectCase64 { public int P64 { get; set; } }

        [JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
        public union TreeUnion(TreeNode, Leaf);

        public sealed class TreeNode
        {
            public int Value { get; set; }

            [JsonRequired]
            public TreeNode? Left { get; set; }
            public TreeNode? Right { get; set; }
        }

        public sealed class Leaf
        {
            public int Value { get; set; }
        }
    }
}
