// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    /// <summary>
    /// Verifies union recognition and the observable contract of
    /// <see cref="JsonTypeInfo{T}.UnionDeconstructor"/> and
    /// <see cref="JsonTypeInfo{T}.UnionConstructor"/> for convention-based unions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same contract applies to both the reflection and source-generated paths, even
    /// though the two implementations differ:
    /// </para>
    /// <list type="bullet">
    ///   <item>The reflection path extracts the case value from a public instance
    ///         <c>object Value</c> property and walks the runtime type's hierarchy to locate
    ///         the nearest declared case (with caching).</item>
    ///   <item>The source generator emits compiler-resolved case patterns ordered most-derived-first.</item>
    /// </list>
    /// <para>
    /// Both implementations must satisfy the same observable contract:
    /// </para>
    /// <list type="bullet">
    ///   <item>The deconstructor returns the *declared* case type — the nearest declared
    ///         ancestor of the case value's runtime type — never the runtime type itself when
    ///         it is a derived non-declared subtype.</item>
    ///   <item>The constructor invokes single-parameter constructors (never conversion operators).</item>
    ///   <item>The constructor dispatches non-declared but assignable case types to the nearest
    ///         declared ancestor's constructor.</item>
    ///   <item>A null value is accepted only when a nullable case exists; otherwise the constructor
    ///         throws.</item>
    /// </list>
    /// </remarks>
    public abstract class UnionTests(JsonSerializerWrapper serializerUnderTest) : SerializerTests(serializerUnderTest)
    {
        public union MixedUnion(int, string, Payload);

        public class Payload
        {
            public string? Name { get; set; }
            public int[]? Values { get; set; }
        }

        [Fact]
        public void CanonicalUnionShape_IsRecognizedWithoutJsonUnionAnnotation()
        {
            JsonTypeInfo<MixedUnion> typeInfo = Serializer.GetTypeInfo<MixedUnion>();

            Assert.Equal(JsonTypeInfoKind.Union, typeInfo.Kind);
            Assert.NotNull(typeInfo.UnionConstructor);
            Assert.NotNull(typeInfo.UnionDeconstructor);
            Assert.Equal(3, typeInfo.UnionCases.Count);
            Assert.Equal(typeof(int), typeInfo.UnionCases[0].CaseType);
            Assert.Equal(typeof(string), typeInfo.UnionCases[1].CaseType);
            Assert.Equal(typeof(Payload), typeInfo.UnionCases[2].CaseType);
        }

        [Fact]
        public async Task CanonicalUnionShape_RoundTripsMixedJsonShapes()
        {
            string intJson = await Serializer.SerializeWrapper(new MixedUnion(42));
            Assert.Equal("42", intJson);
            MixedUnion? intResult = await Serializer.DeserializeWrapper<MixedUnion>("42");
            Assert.Equal(42, GetUnionValue(intResult!));

            string stringJson = await Serializer.SerializeWrapper(new MixedUnion("hello"));
            Assert.Equal("\"hello\"", stringJson);
            MixedUnion? stringResult = await Serializer.DeserializeWrapper<MixedUnion>("\"hello\"");
            Assert.Equal("hello", GetUnionValue(stringResult!));

            Payload payload = new() { Name = "payload", Values = new[] { 1, 2, 3 } };
            string payloadJson = await Serializer.SerializeWrapper(new MixedUnion(payload));
            JsonTestHelper.AssertJsonEqual("""{"Name":"payload","Values":[1,2,3]}""", payloadJson);

            MixedUnion? payloadResult = await Serializer.DeserializeWrapper<MixedUnion>(payloadJson);
            Payload payloadValue = Assert.IsType<Payload>(GetUnionValue(payloadResult!));
            Assert.Equal("payload", payloadValue.Name);
            Assert.Equal(new[] { 1, 2, 3 }, payloadValue.Values);
        }

        public class Foo
        {
            public int Id { get; set; }
        }

        public class Bar
        {
            public string? Name { get; set; }
        }

        public class Baz
        {
            public bool Flag { get; set; }
        }

#pragma warning disable SYSLIB1227
        public union ObjectUnion(Foo, Bar, Baz);
#pragma warning restore SYSLIB1227

        [Fact]
        public async Task ObjectUnionShape_SerializesButDefaultDeserializationIsAmbiguous()
        {
            ObjectUnion value = new(new Foo { Id = 42 });

            string json = await Serializer.SerializeWrapper(value);
            JsonTestHelper.AssertJsonEqual("""{"Id":42}""", json);

            JsonException ex = await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<ObjectUnion>(json));

            Assert.Contains(nameof(ObjectUnion), ex.Message);
            Assert.Contains("Object", ex.Message);
        }

        [JsonUnion(TypeClassifier = typeof(IntStringClassifierFactory))]
        public union UnionWithCustomClassifier(int, string);

        public sealed class IntStringClassifierFactory : JsonTypeClassifierFactory
        {
            public override bool CanClassify(JsonTypeClassifierContext context)
            {
                Assert.Equal(JsonTypeClassifierKind.Union, context.Kind);
                Assert.Equal(typeof(UnionWithCustomClassifier), context.DeclaringType);
                Assert.Equal(2, context.UnionCases.Count);
                Assert.Empty(context.DerivedTypes);
                return true;
            }

            public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
            {
                Assert.Equal(JsonTypeClassifierKind.Union, context.Kind);
                Assert.Equal(typeof(UnionWithCustomClassifier), context.DeclaringType);
                Assert.Equal(2, context.UnionCases.Count);
                return static (ref Utf8JsonReader reader) => reader.TokenType switch
                {
                    JsonTokenType.Number => typeof(int),
                    JsonTokenType.String => typeof(string),
                    _ => null,
                };
            }
        }

        [Fact]
        public async Task PerTypeTypeClassifier_RoundTrips()
        {
            UnionWithCustomClassifier? fromInt = await Serializer.DeserializeWrapper<UnionWithCustomClassifier>("42");
            Assert.NotNull(fromInt);
            Assert.Equal(42, GetUnionValue(fromInt!));

            UnionWithCustomClassifier? fromString = await Serializer.DeserializeWrapper<UnionWithCustomClassifier>("\"hello\"");
            Assert.NotNull(fromString);
            Assert.Equal("hello", GetUnionValue(fromString!));
        }

        // The factory-resolved JsonTypeClassifier delegate must be visible on
        // JsonTypeInfo.TypeClassifier by the time any modifier runs, otherwise modifiers
        // cannot inspect, wrap, or replace the resolved classifier — which is the whole
        // point of contract customization. Validates both reflection and source-gen.
        [Fact]
        public void PerTypeTypeClassifier_IsVisibleToModifier()
        {
            JsonTypeClassifier? observedAtModifierTime = null;

            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(UnionWithCustomClassifier))
                {
                    observedAtModifierTime = typeInfo.TypeClassifier;
                }
            });

            // Trigger configuration of the type info via GetTypeInfo (modifiers run during
            // resolver invocation, before Configure).
            options.MakeReadOnly(populateMissingResolver: true);
            _ = options.GetTypeInfo(typeof(UnionWithCustomClassifier));

            Assert.NotNull(observedAtModifierTime);
        }

        // Case type with a user-defined JsonConverter; runtime cannot classify it
        // without a custom classifier.
        [JsonConverter(typeof(CustomConverter))]
        public class CustomCase
        {
        }

        public sealed class CustomConverter : JsonConverter<CustomCase>
        {
            public override CustomCase? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;
            public override void Write(Utf8JsonWriter writer, CustomCase value, JsonSerializerOptions options) { }
        }

        public class OtherCase
        {
        }

        public sealed class UnionWithOtherCaseOptionsClassifierFactory : JsonTypeClassifierFactory
        {
            public override bool CanClassify(JsonTypeClassifierContext context)
            {
                Assert.Equal(JsonTypeClassifierKind.Union, context.Kind);
                if (context.DeclaringType != typeof(UnionWithCustomConverterCase))
                {
                    return false;
                }

                bool containsOtherCase = false;
                foreach (JsonUnionCaseInfo caseInfo in context.UnionCases)
                {
                    containsOtherCase |= caseInfo.CaseType == typeof(OtherCase);
                }

                Assert.True(containsOtherCase);
                Assert.Empty(context.DerivedTypes);
                return containsOtherCase;
            }

            public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
            {
                Assert.Equal(JsonTypeClassifierKind.Union, context.Kind);
                Assert.Equal(typeof(UnionWithCustomConverterCase), context.DeclaringType);

                return static (ref Utf8JsonReader reader) =>
                    reader.TokenType is JsonTokenType.StartObject ? typeof(OtherCase) : null;
            }
        }

        public sealed class SingleObjectUnionClassifierFactory : JsonTypeClassifierFactory<SingleObjectUnion>
        {
            public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options) =>
                throw new InvalidOperationException("This factory should not be selected.");
        }

        public sealed class GenericUnionWithCustomConverterCaseClassifierFactory : JsonTypeClassifierFactory<UnionWithCustomConverterCase>
        {
            public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
            {
                Assert.Equal(JsonTypeClassifierKind.Union, context.Kind);
                Assert.Equal(typeof(UnionWithCustomConverterCase), context.DeclaringType);

                return static (ref Utf8JsonReader reader) =>
                    reader.TokenType is JsonTokenType.StartObject ? typeof(OtherCase) : null;
            }
        }

#pragma warning disable SYSLIB1227
        public union UnionWithCustomConverterCase(CustomCase, OtherCase);
#pragma warning restore SYSLIB1227

        [Fact]
        public async Task UnionWithCustomConverterCase_NoClassifier_DeserializeThrows()
        {
            // Configure-time succeeds (serialization is allowed). Deserialization throws
            // because a custom converter case can serialize as any JSON value type.
            JsonException ex = await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<UnionWithCustomConverterCase>("{}"));

            Assert.Contains(nameof(UnionWithCustomConverterCase), ex.Message);
            Assert.Contains("custom JsonConverter", ex.Message);
        }

        [Fact]
        public async Task UnionWithCustomConverterCase_NoClassifier_SerializeWorks()
        {
            // Serialization is unaffected by the presence of custom-converter cases —
            // it goes through the deconstructor and the case type's own converter.
            UnionWithCustomConverterCase value = new(new OtherCase());
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal("{}", json);
        }

        [Fact]
        public async Task UnionWithCustomConverterCase_WithClassifier_RoundTrips()
        {
            // With a custom classifier, the runtime bypasses the value-shape map entirely.
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(UnionWithCustomConverterCase))
                {
                    typeInfo.TypeClassifier = static (ref Utf8JsonReader _) => typeof(OtherCase);
                }
            });

            UnionWithCustomConverterCase? value = await Serializer.DeserializeWrapper<UnionWithCustomConverterCase>("{}", options);
            Assert.NotNull(value);
            Assert.IsType<OtherCase>(GetUnionValue(value!));
        }

        [Fact]
        public async Task OptionsTypeClassifier_UsesUnionContextToSelectFactory()
        {
            JsonSerializerOptions options = Serializer.CreateOptions(configure: static options =>
                options.TypeClassifiers.Add(new UnionWithOtherCaseOptionsClassifierFactory()));

            UnionWithCustomConverterCase? value = await Serializer.DeserializeWrapper<UnionWithCustomConverterCase>("{}", options);

            Assert.NotNull(value);
            Assert.IsType<OtherCase>(GetUnionValue(value!));
        }

        [Fact]
        public void OptionsTypeClassifier_IsVisibleToModifier()
        {
            JsonTypeClassifier? observedAtModifierTime = null;

            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(UnionWithCustomConverterCase))
                {
                    observedAtModifierTime = typeInfo.TypeClassifier;
                }
            });

            options.TypeClassifiers.Add(new UnionWithOtherCaseOptionsClassifierFactory());
            options.MakeReadOnly(populateMissingResolver: true);
            _ = options.GetTypeInfo(typeof(UnionWithCustomConverterCase));

            Assert.NotNull(observedAtModifierTime);
        }

        [Fact]
        public async Task GenericOptionsTypeClassifier_AnchorsToDeclaringType()
        {
            JsonSerializerOptions options = Serializer.CreateOptions(configure: static options =>
            {
                options.TypeClassifiers.Add(new SingleObjectUnionClassifierFactory());
                options.TypeClassifiers.Add(new GenericUnionWithCustomConverterCaseClassifierFactory());
            });

            UnionWithCustomConverterCase? value = await Serializer.DeserializeWrapper<UnionWithCustomConverterCase>("{}", options);

            Assert.NotNull(value);
            Assert.IsType<OtherCase>(GetUnionValue(value!));
        }

#pragma warning disable SYSLIB1227
        public union UnionWithIntAndLongCase(int, long);
#pragma warning restore SYSLIB1227

        [Fact]
        public async Task UnionWithOverlappingNumericCases_DeserializeThrows()
        {
            // Configure-time succeeds. Deserialization of a Number token throws because
            // two declared cases share the Number token kind.
            JsonException ex = await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<UnionWithIntAndLongCase>("42"));

            Assert.Contains(nameof(UnionWithIntAndLongCase), ex.Message);
            Assert.Contains("Number", ex.Message);
        }

        [Fact]
        public async Task UnionWithOverlappingNumericCases_SerializeWorks()
        {
            // Serialization is unaffected by the ambiguity — the deconstructor dispatches
            // by case type, not token type.
            UnionWithIntAndLongCase value = new(42);
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal("42", json);
        }

        // Partial ambiguity: Number-token cases overlap (int + long) but String-token case
        // (string) is unique. Deserialization should succeed for unique tokens and only fail
        // for the ambiguous ones — not block the entire union.
#pragma warning disable SYSLIB1227
        public union UnionWithMixedAmbiguity(int, long, string);
#pragma warning restore SYSLIB1227

        [Fact]
        public async Task UnionWithMixedAmbiguity_UniqueTokenDeserializes()
        {
            // String token routes uniquely to the string case despite int/long ambiguity
            // on Number tokens. Configure does not throw.
            UnionWithMixedAmbiguity? value = await Serializer.DeserializeWrapper<UnionWithMixedAmbiguity>("\"hello\"");
            Assert.NotNull(value);
            Assert.Equal("hello", GetUnionValue(value!));
        }

        [Fact]
        public async Task UnionWithMixedAmbiguity_AmbiguousTokenStillThrows()
        {
            JsonException ex = await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<UnionWithMixedAmbiguity>("42"));
            Assert.Contains("Number", ex.Message);
        }

        [Fact]
        public void UnionWithMixedAmbiguity_GetTypeInfoSucceeds()
        {
            // The Configure path must not throw even though the union has an ambiguous token.
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo<UnionWithMixedAmbiguity>();
            Assert.NotNull(typeInfo);
        }

        public class Animal { }
        public class Dog : Animal { }
        public class Lab : Dog { }

        // Declared cases: (Animal, Dog). Topologically sorted: Dog before Animal.
#pragma warning disable SYSLIB1227
        public union HierarchyUnion(Animal, Dog);
#pragma warning restore SYSLIB1227

        [Fact]
        public void Deconstructor_DerivedRuntimeType_ReturnsNearestDeclaredCase()
        {
            JsonTypeInfo<HierarchyUnion> typeInfo = Serializer.GetTypeInfo<HierarchyUnion>();
            Assert.NotNull(typeInfo.UnionDeconstructor);

            Lab lab = new();
            HierarchyUnion union = new HierarchyUnion((Dog)lab);

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(union);

            // Lab is a derived runtime type that is NOT a declared case. The contract requires
            // dispatching to the nearest declared ancestor — Dog.
            Assert.Equal(typeof(Dog), caseType);
            Assert.Same(lab, caseValue);
        }

        [Fact]
        public void Deconstructor_DeclaredCaseRuntimeType_ReturnsCaseType()
        {
            JsonTypeInfo<HierarchyUnion> typeInfo = Serializer.GetTypeInfo<HierarchyUnion>();

            Dog dog = new();
            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(new HierarchyUnion(dog));

            Assert.Equal(typeof(Dog), caseType);
            Assert.Same(dog, caseValue);
        }

        [Fact]
        public void Deconstructor_BaseDeclaredCase_ReturnsBaseCaseType()
        {
            JsonTypeInfo<HierarchyUnion> typeInfo = Serializer.GetTypeInfo<HierarchyUnion>();

            Animal animal = new();
            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(new HierarchyUnion(animal));

            Assert.Equal(typeof(Animal), caseType);
            Assert.Same(animal, caseValue);
        }

        [Fact]
        public void Deconstructor_NullUnionInstance_ReturnsNullCaseTypeAndValue()
        {
            JsonTypeInfo<UserDefinedAttributeOnlyUnion> typeInfo = Serializer.GetTypeInfo<UserDefinedAttributeOnlyUnion>();

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(null!);

            Assert.Null(caseType);
            Assert.Null(caseValue);
        }

        [Fact]
        public void Deconstructor_UnionWithNullValue_NoNullableCase_Throws()
        {
            JsonTypeInfo<HierarchyUnion> typeInfo = Serializer.GetTypeInfo<HierarchyUnion>();

            Assert.ThrowsAny<Exception>(() => typeInfo.UnionDeconstructor!(new HierarchyUnion((Animal)null!)));
        }

        [Fact]
        public void Constructor_DerivedDeclaredCase_RoundTripsThroughDeconstructor()
        {
            JsonTypeInfo<HierarchyUnion> typeInfo = Serializer.GetTypeInfo<HierarchyUnion>();

            Lab lab = new();
            HierarchyUnion union = typeInfo.UnionConstructor!(typeof(Dog), lab);

            // Round-trip through deconstructor verifies the formal case type is preserved.
            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(union);
            Assert.Equal(typeof(Dog), caseType);
            Assert.Same(lab, caseValue);
        }

        public union NullableCaseUnion(string?, int);

        [Fact]
        public void Deconstructor_UnionWithNullValue_NullableCase_ReturnsNullCaseTypeAndValue()
        {
            JsonTypeInfo<NullableCaseUnion> typeInfo = Serializer.GetTypeInfo<NullableCaseUnion>();

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(new NullableCaseUnion((string?)null));

            Assert.Equal(typeof(string), caseType);
            Assert.Null(caseValue);
        }

        [Union]
        public class UserDefinedNullableAttributeOnlyUnion
        {
            public UserDefinedNullableAttributeOnlyUnion(int v) { Value = v; }
            public UserDefinedNullableAttributeOnlyUnion(string? v) { Value = v; }
            public object? Value { get; }
        }

        [Fact]
        public void Deconstructor_NullUnionInstanceAndNullableNullCase_AreDistinct()
        {
            JsonTypeInfo<UserDefinedNullableAttributeOnlyUnion> typeInfo = Serializer.GetTypeInfo<UserDefinedNullableAttributeOnlyUnion>();

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(null!);

            Assert.Null(caseType);
            Assert.Null(caseValue);

            (caseType, caseValue) = typeInfo.UnionDeconstructor!(new UserDefinedNullableAttributeOnlyUnion((string?)null));

            Assert.Equal(typeof(string), caseType);
            Assert.Null(caseValue);
        }

        [Fact]
        public void Constructor_NullValue_NullableCaseExists_BuildsViaNullableCtor()
        {
            JsonTypeInfo<NullableCaseUnion> typeInfo = Serializer.GetTypeInfo<NullableCaseUnion>();
            Assert.NotNull(typeInfo.UnionConstructor);

            NullableCaseUnion union = typeInfo.UnionConstructor!(typeof(string), null);

            Assert.Null(GetUnionValue(union));
        }

        public union NoNullableCaseUnion(int);

        [Fact]
        public void Constructor_NullValue_NoNullableCase_Throws()
        {
            JsonTypeInfo<NoNullableCaseUnion> typeInfo = Serializer.GetTypeInfo<NoNullableCaseUnion>();
            Assert.NotNull(typeInfo.UnionConstructor);

            // Both implementations surface a runtime exception when null is rejected.
            Assert.ThrowsAny<Exception>(() => typeInfo.UnionConstructor!(typeof(int), null));
        }

        public union ValueTypeNullablePairUnion(int, int?);

        [Fact]
        public void NullableAndNonNullableValueTypeCtorOverloads_MergeToSingleNullableCase()
        {
            JsonTypeInfo<ValueTypeNullablePairUnion> typeInfo = Serializer.GetTypeInfo<ValueTypeNullablePairUnion>();

            JsonUnionCaseInfo intCase = Assert.Single(typeInfo.UnionCases);
            Assert.Equal(typeof(int), intCase.CaseType);
            Assert.True(intCase.IsNullable);

            // Non-null still works.
            ValueTypeNullablePairUnion intUnion = typeInfo.UnionConstructor!(typeof(int), 42);
            Assert.Equal(42, GetUnionValue(intUnion));

            ValueTypeNullablePairUnion nullUnion = typeInfo.UnionConstructor!(typeof(int), null);
            Assert.Null(GetUnionValue(nullUnion));
        }

        public class PayloadCase
        {
            public string? Name { get; set; }
            public List<int>? Values { get; set; }
        }

        public union SingleObjectUnion(PayloadCase);

        public class UnionContainer
        {
            public SingleObjectUnion? Union { get; set; }
            public int After { get; set; }
        }

        public class NullableScalarUnionContainer
        {
            public MixedUnion? Union { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(PayloadOrStringClassifierFactory))]
        public union ClassifiedPayloadUnion(PayloadCase, string);

        public sealed class PayloadOrStringClassifierFactory : JsonTypeClassifierFactory
        {
            public override bool CanClassify(JsonTypeClassifierContext context)
            {
                Assert.Equal(JsonTypeClassifierKind.Union, context.Kind);
                return context.DeclaringType == typeof(ClassifiedPayloadUnion);
            }

            public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
            {
                Assert.Equal(JsonTypeClassifierKind.Union, context.Kind);

                return static (ref Utf8JsonReader reader) =>
                {
                    if (reader.TokenType is JsonTokenType.String)
                    {
                        return typeof(string);
                    }

                    if (reader.TokenType is JsonTokenType.StartObject)
                    {
                        while (reader.Read())
                        {
                            if (reader.TokenType is JsonTokenType.PropertyName &&
                                reader.ValueTextEquals(nameof(PayloadCase.Name)))
                            {
                                return typeof(PayloadCase);
                            }
                        }
                    }

                    return null;
                };
            }
        }

        public union AsyncEnumerableUnion(IAsyncEnumerable<int>);

        [Fact]
        public async Task UnionWithDefaultTokenClassification_RoundTripsObjectCaseWithSmallBuffer()
        {
            JsonSerializerOptions options = CreateSmallBufferOptions();
            SingleObjectUnion value = new(new PayloadCase
            {
                Name = "payload",
                Values = new List<int> { 1, 2, 3, 4, 5 }
            });

            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""{"Name":"payload","Values":[1,2,3,4,5]}""", json);

            SingleObjectUnion? result = await Serializer.DeserializeWrapper<SingleObjectUnion>(json, options);

            Assert.NotNull(result);
            PayloadCase payload = Assert.IsType<PayloadCase>(GetUnionValue(result!));
            Assert.Equal("payload", payload.Name);
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, payload.Values);
        }

        [Fact]
        public async Task UnionWithDefaultTokenClassification_RoundTripsObjectCaseAsPropertyWithSmallBuffer()
        {
            JsonSerializerOptions options = CreateSmallBufferOptions();
            UnionContainer value = new()
            {
                Union = new SingleObjectUnion(new PayloadCase
                {
                    Name = "nested",
                    Values = new List<int> { 6, 7, 8 }
                }),
                After = 42
            };

            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""{"Union":{"Name":"nested","Values":[6,7,8]},"After":42}""", json);

            UnionContainer? result = await Serializer.DeserializeWrapper<UnionContainer>(json, options);

            Assert.NotNull(result);
            Assert.Equal(42, result!.After);
            Assert.NotNull(result.Union);
            PayloadCase payload = Assert.IsType<PayloadCase>(GetUnionValue(result.Union!));
            Assert.Equal("nested", payload.Name);
            Assert.Equal(new[] { 6, 7, 8 }, payload.Values);
        }

        [Fact]
        public async Task UnionWithDefaultTokenClassification_RoundTripsNullableScalarCaseAsPropertyWithSmallBuffer()
        {
            JsonSerializerOptions options = CreateSmallBufferOptions();
            NullableScalarUnionContainer value = new()
            {
                Union = new MixedUnion(42),
            };

            string json = await Serializer.SerializeWrapper(value, options);
            Assert.Equal("""{"Union":42}""", json);

            NullableScalarUnionContainer? result = await Serializer.DeserializeWrapper<NullableScalarUnionContainer>(json, options);

            Assert.NotNull(result);
            Assert.True(result!.Union.HasValue);
            MixedUnion union = result.Union.GetValueOrDefault();
            Assert.Equal(42, GetUnionValue(union));

            json = await Serializer.SerializeWrapper(new NullableScalarUnionContainer(), options);
            Assert.Equal("""{"Union":null}""", json);

            result = await Serializer.DeserializeWrapper<NullableScalarUnionContainer>(json, options);

            Assert.NotNull(result);
            Assert.False(result!.Union.HasValue);
        }

        [Fact]
        public async Task UnionWithCustomClassifier_RoundTripsObjectCaseWithSmallBuffer()
        {
            JsonSerializerOptions options = CreateSmallBufferOptions();
            ClassifiedPayloadUnion value = new(new PayloadCase
            {
                Name = "classified",
                Values = new List<int> { 9, 10, 11 }
            });

            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""{"Name":"classified","Values":[9,10,11]}""", json);

            ClassifiedPayloadUnion? result = await Serializer.DeserializeWrapper<ClassifiedPayloadUnion>(json, options);

            Assert.NotNull(result);
            PayloadCase payload = Assert.IsType<PayloadCase>(GetUnionValue(result!));
            Assert.Equal("classified", payload.Name);
            Assert.Equal(new[] { 9, 10, 11 }, payload.Values);
        }

        [Fact]
        public async Task UnionWithAsyncEnumerableCase_SerializesUsingStreamingConverter()
        {
            if (StreamingSerializer?.IsAsyncSerializer != true)
            {
                return;
            }

            JsonSerializerOptions options = CreateSmallBufferOptions();
            string json = await Serializer.SerializeWrapper(new AsyncEnumerableUnion(CreateAsyncEnumerable()), options);

            Assert.Equal("[1,2,3]", json);
        }

        private JsonSerializerOptions CreateSmallBufferOptions()
            => Serializer.CreateOptions(configure: static options => options.DefaultBufferSize = 1);

        private static object? GetUnionValue(IUnion union) => union.Value;

        private static async IAsyncEnumerable<int> CreateAsyncEnumerable()
        {
            yield return 1;
            await Task.Yield();
            yield return 2;
            yield return 3;
        }

        // JsonTypeInfo.Configure validates the final union shape — but a modifier can patch
        // partial state (e.g., null out a delegate and supply a different one) and the change
        // must survive through Configure for serialization to round-trip. This works against
        // any resolver: reflection or source-gen alike.
        [Fact]
        public async Task Configure_AcceptsModifierOverride_OfUnionConstructor()
        {
            int constructorInvocations = 0;

            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(typeInfo =>
            {
                if (typeInfo.Type != typeof(NullableCaseUnion))
                {
                    return;
                }

                JsonTypeInfo<NullableCaseUnion> ti = (JsonTypeInfo<NullableCaseUnion>)typeInfo;
                Func<Type, object?, NullableCaseUnion>? original = ti.UnionConstructor;
                Assert.NotNull(original);

                ti.UnionConstructor = (Type caseType, object? value) =>
                {
                    constructorInvocations++;
                    return original(caseType, value);
                };
            });

            // Number tokens route uniquely to the int case.
            NullableCaseUnion? roundTripped = await Serializer.DeserializeWrapper<NullableCaseUnion>("42", options);
            Assert.NotNull(roundTripped);
            Assert.Equal(42, GetUnionValue(roundTripped!));
            Assert.True(constructorInvocations > 0);
        }

        // Nulling out the constructor delegate via a modifier must cause Configure to throw
        // a clear InvalidOperationException naming the union type. Source-gen and reflection
        // share this validation.
        [Fact]
        public void Configure_ThrowsWhenModifierClearsUnionConstructor()
        {
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(NullableCaseUnion))
                {
                    ((JsonTypeInfo<NullableCaseUnion>)typeInfo).UnionConstructor = null;
                }
            });

            options.MakeReadOnly(populateMissingResolver: true);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => options.GetTypeInfo(typeof(NullableCaseUnion)));

            Assert.Contains(nameof(NullableCaseUnion), ex.Message);
        }

        // Same for the deconstructor delegate.
        [Fact]
        public void Configure_ThrowsWhenModifierClearsUnionDeconstructor()
        {
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(NullableCaseUnion))
                {
                    ((JsonTypeInfo<NullableCaseUnion>)typeInfo).UnionDeconstructor = null;
                }
            });

            options.MakeReadOnly(populateMissingResolver: true);

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => options.GetTypeInfo(typeof(NullableCaseUnion)));

            Assert.Contains(nameof(NullableCaseUnion), ex.Message);
        }

        // [JsonUnion] alone — must be ignored, type behaves as a regular object.
        [JsonUnion]
        public class UserDefinedJsonUnionOnPlainObject
        {
            public int X { get; set; }
            public string? Y { get; set; }
        }

        [Fact]
        public void JsonUnionAttribute_OnNonUnionType_IsIgnored_TypeInfoIsObject()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo<UserDefinedJsonUnionOnPlainObject>();
            Assert.Equal(JsonTypeInfoKind.Object, typeInfo.Kind);
        }

        [Fact]
        public async Task JsonUnionAttribute_OnNonUnionType_IsIgnored_RoundTripsAsObject()
        {
            UserDefinedJsonUnionOnPlainObject value = new() { X = 42, Y = "hello" };
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal("""{"X":42,"Y":"hello"}""", json);

            UserDefinedJsonUnionOnPlainObject? roundTripped = await Serializer.DeserializeWrapper<UserDefinedJsonUnionOnPlainObject>(json);
            Assert.NotNull(roundTripped);
            Assert.Equal(42, roundTripped!.X);
            Assert.Equal("hello", roundTripped.Y);
        }

        // IUnion alone must not make a type a union.
        public class UserDefinedUnionViaIUnion : IUnion
        {
            public UserDefinedUnionViaIUnion(int v) { Value = v; }
            public UserDefinedUnionViaIUnion(string v) { Value = v; }
            public object? Value { get; }
        }

        [Fact]
        public void IUnion_Alone_DoesNotTriggerUnionBehavior()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo<UserDefinedUnionViaIUnion>();
            Assert.Equal(JsonTypeInfoKind.Object, typeInfo.Kind);
        }

#pragma warning disable SYSLIB1228
        [Union]
        public class UserDefinedUnionWithoutValueProperty
        {
            public UserDefinedUnionWithoutValueProperty(int v) { X = v; }
            public int X { get; }
        }
#pragma warning restore SYSLIB1228

        [Fact]
        public void UnionAttribute_WithoutValueProperty_ThrowsAtConfigure()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => Serializer.GetTypeInfo<UserDefinedUnionWithoutValueProperty>());

            Assert.Contains(nameof(UserDefinedUnionWithoutValueProperty), ex.Message);
        }

#pragma warning disable SYSLIB1228
        [Union]
        public class UserDefinedUnconventionalUnion
        {
            public UserDefinedUnconventionalUnion(int x, int y) { Sum = x + y; }
            public int Sum { get; }
            public object Value => Sum;
        }
#pragma warning restore SYSLIB1228

        [Fact]
        public void UnionMissingMetadata_DefaultResolver_ThrowsAtConfigure()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => Serializer.GetTypeInfo<UserDefinedUnconventionalUnion>());

            Assert.Contains(nameof(UserDefinedUnconventionalUnion), ex.Message);
        }

        [Fact]
        public async Task UnionMissingMetadata_ContractCustomization_RescuesAndRoundTrips()
        {
            // The convention path produces an empty union JsonTypeInfo. A modifier patches it up by
            // providing UnionCases plus the deconstructor/constructor delegates.
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(typeInfo =>
            {
                if (typeInfo.Type != typeof(UserDefinedUnconventionalUnion))
                {
                    return;
                }

                JsonTypeInfo<UserDefinedUnconventionalUnion> ti = (JsonTypeInfo<UserDefinedUnconventionalUnion>)typeInfo;
                ti.UnionCases.Add(new JsonUnionCaseInfo(typeof(int)));
                ti.UnionDeconstructor = static (UserDefinedUnconventionalUnion u) => (typeof(int), (object?)u.Sum);
                ti.UnionConstructor = static (Type _, object? value) => new UserDefinedUnconventionalUnion((int)value!, 0);
            });

            UserDefinedUnconventionalUnion value = new(2, 3);
            string json = await Serializer.SerializeWrapper(value, options);
            Assert.Equal("5", json);

            UserDefinedUnconventionalUnion? roundTripped = await Serializer.DeserializeWrapper<UserDefinedUnconventionalUnion>("7", options);
            Assert.NotNull(roundTripped);
            Assert.Equal(7, roundTripped!.Sum);
        }

        [Union]
        public class UserDefinedAttributeOnlyUnion
        {
            public UserDefinedAttributeOnlyUnion(int v) { Value = v; }
            public UserDefinedAttributeOnlyUnion(string v) { Value = v; }
            public object? Value { get; }
        }

        [Fact]
        public async Task UnionAttribute_WithDuckTypedObjectValue_RoundTrips()
        {
            JsonTypeInfo<UserDefinedAttributeOnlyUnion> typeInfo = Serializer.GetTypeInfo<UserDefinedAttributeOnlyUnion>();
            Assert.NotNull(typeInfo.UnionConstructor);
            Assert.NotNull(typeInfo.UnionDeconstructor);

            UserDefinedAttributeOnlyUnion union = typeInfo.UnionConstructor!(typeof(int), 42);
            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(union);

            Assert.Equal(typeof(int), caseType);
            Assert.Equal(42, caseValue);

            string json = await Serializer.SerializeWrapper(union);
            Assert.Equal("42", json);

            UserDefinedAttributeOnlyUnion? roundTripped = await Serializer.DeserializeWrapper<UserDefinedAttributeOnlyUnion>("\"hello\"");
            Assert.NotNull(roundTripped);
            Assert.Equal("hello", roundTripped!.Value);
        }

        // Both ctor and an implicit conversion operator exist. The convention constructor must
        // invoke the ctor and never the operator. We verify by having the operator throw.
        [Union]
        public class UserDefinedCtorVsImplicitOpUnion
        {
            public UserDefinedCtorVsImplicitOpUnion(int v) { Value = v; CtorWasInvoked = true; }
            public static implicit operator UserDefinedCtorVsImplicitOpUnion(int v) =>
                throw new InvalidOperationException("Implicit conversion operator must not be invoked by the convention constructor.");
            public object? Value { get; }
            public bool CtorWasInvoked { get; }
        }

        [Fact]
        public void Constructor_PrefersSingleParamCtorOverImplicitOperator()
        {
            JsonTypeInfo<UserDefinedCtorVsImplicitOpUnion> typeInfo = Serializer.GetTypeInfo<UserDefinedCtorVsImplicitOpUnion>();
            Assert.NotNull(typeInfo.UnionConstructor);

            UserDefinedCtorVsImplicitOpUnion union = typeInfo.UnionConstructor!(typeof(int), 42);

            Assert.True(union.CtorWasInvoked);
            Assert.Equal(42, union.Value);
        }

        [Union]
        public readonly struct UserDefinedValueTypeUnion : IUnion
        {
            public UserDefinedValueTypeUnion(int v) { Value = v; }
            public UserDefinedValueTypeUnion(string v) { Value = v; }
            public object? Value { get; }
        }

        [Fact]
        public void UserDefinedValueTypeUnion_ConventionDelegates_RoundTripCaseValue()
        {
            JsonTypeInfo<UserDefinedValueTypeUnion> typeInfo = Serializer.GetTypeInfo<UserDefinedValueTypeUnion>();

            UserDefinedValueTypeUnion union = typeInfo.UnionConstructor!(typeof(int), 42);
            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(union);

            Assert.Equal(typeof(int), caseType);
            Assert.Equal(42, caseValue);
        }

        // Hand-rolled discriminated union using the "non-boxing access pattern" described in
        // https://github.com/dotnet/csharplang/blob/main/proposals/unions.md#non-boxing-access-members.
        // The struct keeps a discriminator field, exposes the boxed payload through IUnion.Value
        // (which is what STJ deconstructs), and exposes the user-facing payload through typed
        // TryGetValue(out T) overloads (which STJ does not consume). The Tag property lets tests
        // assert which case constructor STJ selected on the way back in.
#pragma warning disable SYSLIB1227
        [Union]
        public readonly struct CustomDiscriminatedAnimalUnion : IUnion
        {
            public CustomDiscriminatedAnimalUnion(Animal animal) { Tag = 0; Value = animal; }
            public CustomDiscriminatedAnimalUnion(Dog dog) { Tag = 1; Value = dog; }

            public int Tag { get; }
            public object? Value { get; }
            public bool HasValue => Value is not null;

            public bool TryGetValue(out Animal? animal)
            {
                if (Tag == 0 && Value is Animal a)
                {
                    animal = a;
                    return true;
                }

                animal = null;
                return false;
            }

            public bool TryGetValue(out Dog? dog)
            {
                if (Tag == 1 && Value is Dog d)
                {
                    dog = d;
                    return true;
                }

                dog = null;
                return false;
            }
        }
#pragma warning restore SYSLIB1227

        // Non-boxing pattern + most-derived-declared-case wins: a Dog runtime value flows
        // through the Dog case constructor, never the Animal one, on the way out and back in.
        [Fact]
        public void CustomDiscriminatedAnimalUnion_DogRuntimeType_DispatchesToDogCtor()
        {
            JsonTypeInfo<CustomDiscriminatedAnimalUnion> typeInfo = Serializer.GetTypeInfo<CustomDiscriminatedAnimalUnion>();
            Assert.NotNull(typeInfo.UnionDeconstructor);
            Assert.NotNull(typeInfo.UnionConstructor);

            Dog dog = new();
            CustomDiscriminatedAnimalUnion original = new(dog);
            Assert.Equal(1, original.Tag);

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(original);
            Assert.Equal(typeof(Dog), caseType);
            Assert.Same(dog, caseValue);

            CustomDiscriminatedAnimalUnion rebuilt = typeInfo.UnionConstructor!(caseType!, caseValue);
            Assert.Equal(1, rebuilt.Tag);
            Assert.True(rebuilt.TryGetValue(out Dog? rebuiltDog));
            Assert.Same(dog, rebuiltDog);
        }

        // When a [Union] type defines `bool TryGetValue(out CaseType)` overloads, the
        // reflection-based deconstructor chains those overloads so the deconstructed caseType is
        // selected by the user-controlled tag rather than the runtime type of the boxed value.
        // So a Dog upcast through the (Animal) constructor decomposes as typeof(Animal). The
        // constructor delegate then round-trips on the deconstructed caseType, so feeding
        // (typeof(Animal), dog) back in selects the (Animal) case constructor and preserves the
        // user-controlled Tag=0.
        //
        // The source generator currently emits a plain `value switch` for both delegates, which
        // dispatches on the runtime type of the boxed value and does not honor TryGetValue
        // overloads. Aligning source-gen with the reflection convention is tracked separately,
        // so the source-generated path is skipped here.
        [Fact]
        public void CustomDiscriminatedAnimalUnion_UpcastDog_DispatchesByTagConvention()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                return;
            }

            JsonTypeInfo<CustomDiscriminatedAnimalUnion> typeInfo = Serializer.GetTypeInfo<CustomDiscriminatedAnimalUnion>();

            Dog dog = new();
            CustomDiscriminatedAnimalUnion original = new(animal: dog);
            Assert.Equal(0, original.Tag);

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(original);
            Assert.Equal(typeof(Animal), caseType);
            Assert.Same(dog, caseValue);

            CustomDiscriminatedAnimalUnion rebuilt = typeInfo.UnionConstructor!(caseType!, caseValue);
            Assert.Equal(0, rebuilt.Tag);
            Assert.True(rebuilt.TryGetValue(out Animal? rebuiltAnimal));
            Assert.Same(dog, rebuiltAnimal);
        }

        // Same Tag-keyed convention as above: a Lab (not itself a declared case) passed through
        // the (Animal) constructor decomposes as typeof(Animal) because TryGetValue(out Animal)
        // returns true while TryGetValue(out Dog) returns false. The round-trip again funnels
        // through the (Animal) case constructor since dispatch is keyed on the deconstructed
        // caseType, preserving Tag=0.
        //
        // The source-generated `value switch` does not honor TryGetValue and is skipped here for
        // the same reason as above.
        [Fact]
        public void CustomDiscriminatedAnimalUnion_NonDeclaredDerivedRuntimeType_DispatchesByTagConvention()
        {
            if (Serializer.IsSourceGeneratedSerializer)
            {
                return;
            }

            JsonTypeInfo<CustomDiscriminatedAnimalUnion> typeInfo = Serializer.GetTypeInfo<CustomDiscriminatedAnimalUnion>();

            Lab lab = new();
            CustomDiscriminatedAnimalUnion original = new(animal: lab);
            Assert.Equal(0, original.Tag);

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(original);
            Assert.Equal(typeof(Animal), caseType);
            Assert.Same(lab, caseValue);

            CustomDiscriminatedAnimalUnion rebuilt = typeInfo.UnionConstructor!(caseType!, caseValue);
            Assert.Equal(0, rebuilt.Tag);
            Assert.True(rebuilt.TryGetValue(out Animal? rebuiltAnimal));
            Assert.Same(lab, rebuiltAnimal);
        }

        // Sibling of CustomDiscriminatedAnimalUnion that intentionally OMITS TryGetValue overloads.
        // Without the convention, the reflection deconstructor and the source generator both fall
        // back to runtime-type dispatch using union.Value, picking the most-derived declared case
        // (the rule reasserted in https://github.com/dotnet/runtime/pull/128162#discussion_r3370238345).
#pragma warning disable SYSLIB1227
        [Union]
        public readonly struct CustomDiscriminatedAnimalUnion_NoConvention : IUnion
        {
            public CustomDiscriminatedAnimalUnion_NoConvention(Animal animal) { Tag = 0; Value = animal; }
            public CustomDiscriminatedAnimalUnion_NoConvention(Dog dog) { Tag = 1; Value = dog; }

            public int Tag { get; }
            public object? Value { get; }
        }
#pragma warning restore SYSLIB1227

        // Without TryGetValue conventions, runtime-type dispatch wins regardless of which
        // constructor was used: an Animal-via-Dog upcast still flows back through the Dog case.
        [Theory]
        [InlineData(0)] // Dog constructed via the (Animal) ctor — upcast scenario.
        [InlineData(1)] // Dog constructed via the (Dog) ctor — direct scenario.
        public void CustomDiscriminatedAnimalUnion_NoConvention_DogRuntimeType_DispatchesByRuntimeType(int tag)
        {
            JsonTypeInfo<CustomDiscriminatedAnimalUnion_NoConvention> typeInfo = Serializer.GetTypeInfo<CustomDiscriminatedAnimalUnion_NoConvention>();

            Dog dog = new();
            CustomDiscriminatedAnimalUnion_NoConvention original = tag == 1
                ? new(dog: dog)
                : new(animal: dog);

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(original);
            Assert.Equal(typeof(Dog), caseType);
            Assert.Same(dog, caseValue);

            CustomDiscriminatedAnimalUnion_NoConvention rebuilt = typeInfo.UnionConstructor!(caseType!, caseValue);
            Assert.Same(dog, rebuilt.Value);
        }

        // Without TryGetValue conventions, a derived runtime type (Lab) that is not itself a
        // declared case still flows through its nearest declared ancestor (Dog) — most-derived-wins.
        [Fact]
        public void CustomDiscriminatedAnimalUnion_NoConvention_DerivedRuntimeType_DispatchesToMostDerivedDeclaredCase()
        {
            JsonTypeInfo<CustomDiscriminatedAnimalUnion_NoConvention> typeInfo = Serializer.GetTypeInfo<CustomDiscriminatedAnimalUnion_NoConvention>();

            Lab lab = new();
            CustomDiscriminatedAnimalUnion_NoConvention original = new(animal: lab);

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(original);
            Assert.Equal(typeof(Dog), caseType);
            Assert.Same(lab, caseValue);
        }

        // Without TryGetValue conventions, a plain Animal runtime value selects the Animal case.
        [Fact]
        public void CustomDiscriminatedAnimalUnion_NoConvention_AnimalRuntimeType_DispatchesToAnimalCase()
        {
            JsonTypeInfo<CustomDiscriminatedAnimalUnion_NoConvention> typeInfo = Serializer.GetTypeInfo<CustomDiscriminatedAnimalUnion_NoConvention>();

            Animal animal = new();
            CustomDiscriminatedAnimalUnion_NoConvention original = new(animal: animal);

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(original);
            Assert.Equal(typeof(Animal), caseType);
            Assert.Same(animal, caseValue);
        }

        // Hand-rolled discriminated union with disjoint JSON value shapes (number vs string),
        // exercising the full SerializeWrapper/DeserializeWrapper path. The non-boxing TryGetValue
        // accessors aren't consumed by STJ, but the Tag/Value pair lets the test assert that the
        // round-tripped instance came from the same case constructor that originally produced it.
        [Union]
        public readonly struct CustomDiscriminatedScalarUnion : IUnion
        {
            public CustomDiscriminatedScalarUnion(int v) { Tag = 0; Value = v; }
            public CustomDiscriminatedScalarUnion(string v) { Tag = 1; Value = v; }

            public int Tag { get; }
            public object? Value { get; }

            public bool TryGetValue(out int v)
            {
                if (Tag == 0 && Value is int i)
                {
                    v = i;
                    return true;
                }

                v = default;
                return false;
            }

            public bool TryGetValue(out string? v)
            {
                if (Tag == 1 && Value is string s)
                {
                    v = s;
                    return true;
                }

                v = null;
                return false;
            }
        }

        [Fact]
        public async Task CustomDiscriminatedScalarUnion_IntCase_RoundTripsViaIntCtor()
        {
            CustomDiscriminatedScalarUnion original = new(42);
            Assert.Equal(0, original.Tag);

            string json = await Serializer.SerializeWrapper(original);
            Assert.Equal("42", json);

            CustomDiscriminatedScalarUnion rebuilt = await Serializer.DeserializeWrapper<CustomDiscriminatedScalarUnion>("42");
            Assert.Equal(0, rebuilt.Tag);
            Assert.True(rebuilt.TryGetValue(out int rebuiltInt));
            Assert.Equal(42, rebuiltInt);
        }

        [Fact]
        public async Task CustomDiscriminatedScalarUnion_StringCase_RoundTripsViaStringCtor()
        {
            CustomDiscriminatedScalarUnion original = new("hello");
            Assert.Equal(1, original.Tag);

            string json = await Serializer.SerializeWrapper(original);
            Assert.Equal("\"hello\"", json);

            CustomDiscriminatedScalarUnion rebuilt = await Serializer.DeserializeWrapper<CustomDiscriminatedScalarUnion>("\"hello\"");
            Assert.Equal(1, rebuilt.Tag);
            Assert.True(rebuilt.TryGetValue(out string? rebuiltString));
            Assert.Equal("hello", rebuiltString);
        }

        // Disjoint case types (int, string) — topological sorting has nothing to disambiguate, so
        // TryGetValue convention and .Value-based dispatch always agree on the case. This test
        // exercises the deconstructor + constructor delegates directly (no JSON), confirming that
        // the round trip is symmetric: Tag is preserved because the boxed value's runtime type is
        // the only thing distinguishing the cases on both the way out and the way back in.
        [Theory]
        [InlineData(0)] // int case, Tag = 0
        [InlineData(1)] // string case, Tag = 1
        public void CustomDiscriminatedScalarUnion_DirectDelegate_DisjointTypesPreserveTag(int tag)
        {
            JsonTypeInfo<CustomDiscriminatedScalarUnion> typeInfo = Serializer.GetTypeInfo<CustomDiscriminatedScalarUnion>();

            CustomDiscriminatedScalarUnion original = tag == 0
                ? new(42)
                : new("hello");

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(original);
            Assert.Equal(tag == 0 ? typeof(int) : typeof(string), caseType);
            Assert.Equal(tag == 0 ? (object)42 : "hello", caseValue);

            CustomDiscriminatedScalarUnion rebuilt = typeInfo.UnionConstructor!(caseType!, caseValue);
            Assert.Equal(tag, rebuilt.Tag);
            Assert.Equal(original.Value, rebuilt.Value);
        }

        // Disjoint reference case types with no shared base — same property as the scalar union
        // above: there is no inheritance for topological sorting to flatten, so TryGetValue and
        // value-runtime-type dispatch produce the same selection. Confirms Tag is preserved on
        // round trip through the deconstructor + constructor delegates.
        public sealed class Tree
        {
            public int Height { get; set; }
        }

        public sealed class Rock
        {
            public string? Kind { get; set; }
        }

#pragma warning disable SYSLIB1227
        [Union]
        public readonly struct CustomDiscriminatedFlora : IUnion
        {
            public CustomDiscriminatedFlora(Tree tree) { Tag = 0; Value = tree; }
            public CustomDiscriminatedFlora(Rock rock) { Tag = 1; Value = rock; }

            public int Tag { get; }
            public object? Value { get; }

            public bool TryGetValue(out Tree? tree)
            {
                if (Tag == 0 && Value is Tree t)
                {
                    tree = t;
                    return true;
                }

                tree = null;
                return false;
            }

            public bool TryGetValue(out Rock? rock)
            {
                if (Tag == 1 && Value is Rock r)
                {
                    rock = r;
                    return true;
                }

                rock = null;
                return false;
            }
        }
#pragma warning restore SYSLIB1227

        [Theory]
        [InlineData(0)] // Tree case, Tag = 0
        [InlineData(1)] // Rock case, Tag = 1
        public void CustomDiscriminatedFlora_DirectDelegate_DisjointReferenceTypesPreserveTag(int tag)
        {
            JsonTypeInfo<CustomDiscriminatedFlora> typeInfo = Serializer.GetTypeInfo<CustomDiscriminatedFlora>();

            Tree tree = new() { Height = 7 };
            Rock rock = new() { Kind = "Granite" };

            CustomDiscriminatedFlora original = tag == 0 ? new(tree) : new(rock);

            (Type? caseType, object? caseValue) = typeInfo.UnionDeconstructor!(original);
            Assert.Equal(tag == 0 ? typeof(Tree) : typeof(Rock), caseType);
            Assert.Same(tag == 0 ? (object)tree : rock, caseValue);

            CustomDiscriminatedFlora rebuilt = typeInfo.UnionConstructor!(caseType!, caseValue);
            Assert.Equal(tag, rebuilt.Tag);
            Assert.Same(original.Value, rebuilt.Value);
        }

        public enum Color { Red, Green, Blue }

        public union NullableEnumUnion(Color?, string);

        [Fact]
        public void NullableEnumUnionCase_SchemaIncludesNullInTypeArray()
        {
            JsonTypeInfo<NullableEnumUnion> typeInfo = Serializer.GetTypeInfo<NullableEnumUnion>();
            JsonNode schema = typeInfo.GetJsonSchemaAsNode();

            JsonArray anyOf = Assert.IsType<JsonArray>(schema["anyOf"]);
            Assert.Equal(2, anyOf.Count);

            JsonNode? nullableEnumCase = anyOf.FirstOrDefault(node =>
                node?["type"] is JsonArray types &&
                types.Any(t => (string?)t == "integer"));
            Assert.NotNull(nullableEnumCase);

            JsonArray typeArray = Assert.IsType<JsonArray>(nullableEnumCase["type"]);
            Assert.Contains("integer", typeArray.Select(node => (string?)node));
            Assert.Contains("null", typeArray.Select(node => (string?)node));
        }
        #region Recursive unions

        public union RecursiveNat(bool, RecursiveNat);

        [Fact]
        public async Task RecursiveUnion_TripleNested_SerializesAsLeafValue()
        {
            var tripleNested = new RecursiveNat(new RecursiveNat(new RecursiveNat(true)));

            string json = await Serializer.SerializeWrapper(tripleNested);
            Assert.Equal("true", json);
        }

        [Fact]
        public async Task RecursiveUnion_DoubleNested_SerializesAsLeafValue()
        {
            var doubleNested = new RecursiveNat(new RecursiveNat(false));

            string json = await Serializer.SerializeWrapper(doubleNested);
            Assert.Equal("false", json);
        }

        [Fact]
        public async Task RecursiveUnion_Deserialize_YieldsSingleLevelWrapper()
        {
            RecursiveNat? result = await Serializer.DeserializeWrapper<RecursiveNat>("true");

            Assert.NotNull(result);
            Assert.Equal(true, GetUnionValue(result!));
        }

        [Fact]
        public async Task RecursiveUnion_RoundTrip_FlattensNesting()
        {
            var tripleNested = new RecursiveNat(new RecursiveNat(new RecursiveNat(true)));

            string json = await Serializer.SerializeWrapper(tripleNested);
            Assert.Equal("true", json);

            RecursiveNat? deserialized = await Serializer.DeserializeWrapper<RecursiveNat>(json);
            Assert.NotNull(deserialized);
            Assert.IsType<bool>(GetUnionValue(deserialized!));
            Assert.Equal(true, GetUnionValue(deserialized!));
        }

        #endregion

        #region Cyclic object graph

        [Union]
        public class SelfReferentialUnion : IUnion
        {
            public SelfReferentialUnion(string s) { Value = s; }
            public SelfReferentialUnion(SelfReferentialUnion inner) { Value = inner; }
            public object? Value { get; set; }
        }

        [Fact]
        public void CyclicUnion_Serialize_ThrowsJsonExceptionAtMaxDepth()
        {
            var union = new SelfReferentialUnion("seed");
            union.Value = union; // cyclic reference

            var options = new JsonSerializerOptions(Serializer.DefaultOptions)
            {
                MaxDepth = 2,
            };

            Assert.Throws<JsonException>(() =>
                JsonSerializer.Serialize(union, options));
        }

        [Fact]
        public async Task SelfReferentialUnion_NonCyclic_RoundTrips()
        {
            var inner = new SelfReferentialUnion("hello");
            var outer = new SelfReferentialUnion(inner);

            string json = await Serializer.SerializeWrapper(outer);
            Assert.Equal("\"hello\"", json);

            SelfReferentialUnion? deserialized = await Serializer.DeserializeWrapper<SelfReferentialUnion>(json);
            Assert.NotNull(deserialized);
            Assert.Equal("hello", deserialized!.Value);
        }

        #endregion
    }
}
