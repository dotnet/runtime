// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PolymorphicTests
    {
        protected virtual JsonSerializerOptions ClosedTypeInferenceOptions =>
            field ??= new(Serializer.DefaultOptions)
            {
                InferClosedTypePolymorphism = true,
            };

        private static string[] GetInferredDiscriminators(JsonSerializerOptions options, Type baseType)
        {
            JsonTypeInfo typeInfo = options.GetTypeInfo(baseType);
            Assert.NotNull(typeInfo.PolymorphismOptions);
            return typeInfo.PolymorphismOptions.DerivedTypes
                .Select(derivedType => (string)derivedType.TypeDiscriminator!)
                .OrderBy(discriminator => discriminator, StringComparer.Ordinal)
                .ToArray();
        }

        public static IEnumerable<object[]> BasicClosedHierarchyData()
        {
            yield return new object[]
            {
                new ClosedCircle { Name = "circle", Radius = 3 },
                """{"$type":"ClosedCircle","Radius":3,"Name":"circle"}""",
            };
            yield return new object[]
            {
                new ClosedSquare { Name = "square", SideLength = 4 },
                """{"$type":"ClosedSquare","SideLength":4,"Name":"square"}""",
            };
            yield return new object[]
            {
                new ClosedTriangle { Name = "triangle", BaseLength = 5, Height = 6 },
                """{"$type":"ClosedTriangle","BaseLength":5,"Height":6,"Name":"triangle"}""",
            };
        }

        [Theory]
        [MemberData(nameof(BasicClosedHierarchyData))]
        public async Task ClosedTypeInference_BasicHierarchy_EmitsAndReadsTypeDiscriminator(
            ClosedShape value,
            string expectedJson)
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            Type expectedDerivedType = value.GetType();

            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);

            ClosedShape roundtripped = await Serializer.DeserializeWrapper<ClosedShape>(json, options);
            Assert.IsType(expectedDerivedType, roundtripped);

            string roundtrippedJson = await Serializer.SerializeWrapper(roundtripped, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, roundtrippedJson);
        }

        [Fact]
        public void ClosedTypeInference_InferredDiscriminatorsMatchSimpleTypeName()
        {
            Assert.Equal(
                new[] { "ClosedCircle", "ClosedSquare", "ClosedTriangle" },
                GetInferredDiscriminators(ClosedTypeInferenceOptions, typeof(ClosedShape)));
            Assert.Equal(
                new[] { nameof(ClosedBag<int>), nameof(ClosedBox<int>) },
                GetInferredDiscriminators(ClosedTypeInferenceOptions, typeof(ClosedContainer<int>)));
        }

        [Fact]
        public async Task ClosedTypeInference_PreservesDerivedTypeProperties()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;

            ClosedPayload text = new ClosedTextPayload { Id = "text", Text = "hello" };
            string textJson = await Serializer.SerializeWrapper(text, options);
            JsonTestHelper.AssertJsonEqual(
                """{"$type":"ClosedTextPayload","Text":"hello","Id":"text"}""",
                textJson);
            ClosedPayload textRoundtripped = await Serializer.DeserializeWrapper<ClosedPayload>(textJson, options);
            ClosedTextPayload textResult = Assert.IsType<ClosedTextPayload>(textRoundtripped);
            Assert.Equal("text", textResult.Id);
            Assert.Equal("hello", textResult.Text);

            ClosedPayload number = new ClosedNumberPayload { Id = "number", Number = 42 };
            string numberJson = await Serializer.SerializeWrapper(number, options);
            JsonTestHelper.AssertJsonEqual(
                """{"$type":"ClosedNumberPayload","Number":42,"Id":"number"}""",
                numberJson);
            ClosedPayload numberRoundtripped = await Serializer.DeserializeWrapper<ClosedPayload>(numberJson, options);
            ClosedNumberPayload numberResult = Assert.IsType<ClosedNumberPayload>(numberRoundtripped);
            Assert.Equal("number", numberResult.Id);
            Assert.Equal(42, numberResult.Number);
        }

        [Fact]
        public async Task ClosedTypeInference_CollectionOfClosedBase_InfersEachElement()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;

            List<ClosedShape> value =
            [
                new ClosedCircle { Name = "circle", Radius = 3 },
                new ClosedSquare { Name = "square", SideLength = 4 },
                new ClosedTriangle { Name = "triangle", BaseLength = 5, Height = 6 },
            ];
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual(
                """
                [
                    {"$type":"ClosedCircle","Radius":3,"Name":"circle"},
                    {"$type":"ClosedSquare","SideLength":4,"Name":"square"},
                    {"$type":"ClosedTriangle","BaseLength":5,"Height":6,"Name":"triangle"}
                ]
                """,
                json);

            List<ClosedShape> roundtripped = await Serializer.DeserializeWrapper<List<ClosedShape>>(json, options);
            Assert.Collection(
                roundtripped,
                element =>
                {
                    ClosedCircle circle = Assert.IsType<ClosedCircle>(element);
                    Assert.Equal("circle", circle.Name);
                    Assert.Equal(3, circle.Radius);
                },
                element =>
                {
                    ClosedSquare square = Assert.IsType<ClosedSquare>(element);
                    Assert.Equal("square", square.Name);
                    Assert.Equal(4, square.SideLength);
                },
                element =>
                {
                    ClosedTriangle triangle = Assert.IsType<ClosedTriangle>(element);
                    Assert.Equal("triangle", triangle.Name);
                    Assert.Equal(5, triangle.BaseLength);
                    Assert.Equal(6, triangle.Height);
                });
        }

        [Fact]
        public async Task ClosedTypeInference_NestedClosedProperty_InfersAlongsideRegularProperties()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;

            ClosedShapeHolder value = new()
            {
                Name = "holder",
                Shape = new ClosedSquare { Name = "nested-square", SideLength = 4 },
            };
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual(
                """{"Name":"holder","Shape":{"$type":"ClosedSquare","SideLength":4,"Name":"nested-square"}}""",
                json);

            ClosedShapeHolder roundtripped = await Serializer.DeserializeWrapper<ClosedShapeHolder>(json, options);
            Assert.Equal("holder", roundtripped.Name);
            ClosedSquare square = Assert.IsType<ClosedSquare>(roundtripped.Shape);
            Assert.Equal("nested-square", square.Name);
            Assert.Equal(4, square.SideLength);
        }

        [Fact]
        public async Task ClosedTypeInference_DeserializeUnknownDiscriminator_Throws()
        {
            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<ClosedShape>(
                    """{"$type":"Nonexistent"}""",
                    ClosedTypeInferenceOptions));
        }

        [Fact]
        public async Task ClosedTypeInference_FlagDisabled_DoesNotInferPolymorphism()
        {
            JsonSerializerOptions options = Serializer.DefaultOptions;
            ClosedShape value = new ClosedCircle { Name = "circle", Radius = 3 };
            string json = await Serializer.SerializeWrapper(value, options);

            JsonTestHelper.AssertJsonEqual("""{"Name":"circle"}""", json);
            Assert.Null(options.GetTypeInfo(typeof(ClosedShape)).PolymorphismOptions);
        }

        [Fact]
        public void ClosedTypeInference_EmptyDerivedTypes_IsInert()
        {
            Assert.Null(ClosedTypeInferenceOptions.GetTypeInfo(typeof(ClosedEmptyBase)).PolymorphismOptions);
        }

        [Fact]
        public async Task ClosedTypeInference_PlainAbstractClass_IsNotInferred()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            Assert.Null(options.GetTypeInfo(typeof(PlainAbstractBase)).PolymorphismOptions);

            PlainAbstractBase value = new PlainAbstractDerived();
            string json = await Serializer.SerializeWrapper(value, options);
            Assert.DoesNotContain("$type", json);
        }

        public static IEnumerable<object[]> GenericClosedHierarchyData()
        {
            yield return new object[]
            {
                typeof(ClosedContainer<string>),
                new ClosedBox<string> { BaseValue = "base-string", Value = "x" },
                """{"$type":"ClosedBox","Value":"x","BaseValue":"base-string"}""",
                typeof(ClosedBox<string>),
            };
            yield return new object[]
            {
                typeof(ClosedContainer<int>),
                new ClosedBox<int> { BaseValue = 7, Value = 42 },
                """{"$type":"ClosedBox","Value":42,"BaseValue":7}""",
                typeof(ClosedBox<int>),
            };
            yield return new object[]
            {
                typeof(ClosedContainer<int>),
                new ClosedBag<int> { BaseValue = 3, Items = new() { 1, 2 } },
                """{"$type":"ClosedBag","Items":[1,2],"BaseValue":3}""",
                typeof(ClosedBag<int>),
            };
            yield return new object[]
            {
                typeof(ClosedContainer<List<int>>),
                new ClosedBox<List<int>>
                {
                    BaseValue = new() { 4, 5 },
                    Value = new() { 1, 2, 3 },
                },
                """{"$type":"ClosedBox","Value":[1,2,3],"BaseValue":[4,5]}""",
                typeof(ClosedBox<List<int>>),
            };
            yield return new object[]
            {
                typeof(ClosedPair<string, int>),
                new ClosedEntry<string, int>
                {
                    BaseKey = "base",
                    BaseValue = 5,
                    Key = "k",
                    Value = 7,
                },
                """{"$type":"ClosedEntry","Key":"k","Value":7,"BaseKey":"base","BaseValue":5}""",
                typeof(ClosedEntry<string, int>),
            };
            yield return new object[]
            {
                typeof(ClosedWrappedBase<List<string>>),
                new ClosedWrappedDerived<string>
                {
                    BaseValue = new() { "base" },
                    Data = new() { "a", "b" },
                },
                """{"$type":"ClosedWrappedDerived","Data":["a","b"],"BaseValue":["base"]}""",
                typeof(ClosedWrappedDerived<string>),
            };
            yield return new object[]
            {
                typeof(ClosedArrayBase<int[]>),
                new ClosedArrayDerived<int>
                {
                    BaseValue = new[] { 4, 5 },
                    Values = new[] { 1, 2, 3 },
                },
                """{"$type":"ClosedArrayDerived","Values":[1,2,3],"BaseValue":[4,5]}""",
                typeof(ClosedArrayDerived<int>),
            };
            yield return new object[]
            {
                typeof(ClosedUnspeakableBase<int[]>),
                new ClosedUnspeakableIdentityDerived<int[]>(),
                """{"$type":"ClosedUnspeakableIdentityDerived"}""",
                typeof(ClosedUnspeakableIdentityDerived<int[]>),
            };
            yield return new object[]
            {
                typeof(ClosedUnspeakableBase<int[]>),
                new ClosedUnspeakableArrayDerived<int>(),
                """{"$type":"ClosedUnspeakableArrayDerived"}""",
                typeof(ClosedUnspeakableArrayDerived<int>),
            };
            yield return new object[]
            {
                typeof(ClosedReorderedBase<int, string>),
                new ClosedReorderedDerived<string, int>
                {
                    BaseFirst = 7,
                    BaseSecond = "base",
                    Left = "left",
                    Right = 42,
                },
                """{"$type":"ClosedReorderedDerived","Left":"left","Right":42,"BaseFirst":7,"BaseSecond":"base"}""",
                typeof(ClosedReorderedDerived<string, int>),
            };
            yield return new object[]
            {
                typeof(ClosedPartialBase<string, int>),
                new ClosedPartialDerived<string>
                {
                    BaseFirst = "base",
                    BaseSecond = 11,
                    Value = "hello",
                },
                """{"$type":"ClosedPartialDerived","Value":"hello","BaseFirst":"base","BaseSecond":11}""",
                typeof(ClosedPartialDerived<string>),
            };
            yield return new object[]
            {
                typeof(ClosedKvpBase<KeyValuePair<string, int>>),
                new ClosedKvpDerived<int>
                {
                    BaseValue = new("base", 5),
                    Pair = new("k", 99),
                },
                """{"$type":"ClosedKvpDerived","Pair":{"Key":"k","Value":99},"BaseValue":{"Key":"base","Value":5}}""",
                typeof(ClosedKvpDerived<int>),
            };
            yield return new object[]
            {
                typeof(ClosedTupleBase<(int, string)>),
                new ClosedTupleDerived<int, string> { BaseMarker = "base", Label = "pair" },
                """{"$type":"ClosedTupleDerived","Label":"pair","BaseMarker":"base"}""",
                typeof(ClosedTupleDerived<int, string>),
            };
            yield return new object[]
            {
                typeof(ClosedNestedArgBase<ClosedNestedOuter<string>.NestedBox<int>>),
                new ClosedNestedArgDerived<string> { BaseMarker = "base", Marker = "nested" },
                """{"$type":"ClosedNestedArgDerived","Marker":"nested","BaseMarker":"base"}""",
                typeof(ClosedNestedArgDerived<string>),
            };
            yield return new object[]
            {
                typeof(ClosedConstrainedBase<List<string>>),
                new ClosedConstrainedDerived<List<string>>
                {
                    BaseValue = new() { "base" },
                    Items = new() { "hello" },
                },
                """{"$type":"ClosedConstrainedDerived","Items":["hello"],"BaseValue":["base"]}""",
                typeof(ClosedConstrainedDerived<List<string>>),
            };
            yield return new object[]
            {
                typeof(ClosedNestedDerivedBase<int>),
                new ClosedNestedDerivedBase<int>.Derived { BaseValue = 7, Value = 42 },
                $$"""{"$type":"{{typeof(ClosedNestedDerivedBase<int>.Derived).Name}}","Value":42,"BaseValue":7}""",
                typeof(ClosedNestedDerivedBase<int>.Derived),
            };
            yield return new object[]
            {
                typeof(ClosedMixedBase<int>),
                new ClosedMixedOpenDerived<int> { BaseValue = 1, Marker = "open" },
                """{"$type":"ClosedMixedOpenDerived","Marker":"open","BaseValue":1}""",
                typeof(ClosedMixedOpenDerived<int>),
            };
            yield return new object[]
            {
                typeof(ClosedMixedBase<int>),
                new ClosedMixedFixedDerived { BaseValue = 2, Marker = "fixed" },
                """{"$type":"ClosedMixedFixedDerived","Marker":"fixed","BaseValue":2}""",
                typeof(ClosedMixedFixedDerived),
            };
            yield return new object[]
            {
                typeof(ClosedDeepJaggedBase<List<int[][][]>>),
                new ClosedDeepJaggedDerived<int> { BaseMarker = "base", Marker = "deep" },
                """{"$type":"ClosedDeepJaggedDerived","Marker":"deep","BaseMarker":"base"}""",
                typeof(ClosedDeepJaggedDerived<int>),
            };
            yield return new object[]
            {
                typeof(ClosedRepeatedBase<int, int>),
                new ClosedRepeatedDerived<int> { First = 1, Second = 2, Marker = "repeated" },
                """{"$type":"ClosedRepeatedDerived","Marker":"repeated","First":1,"Second":2}""",
                typeof(ClosedRepeatedDerived<int>),
            };
        }

        [Theory]
        [MemberData(nameof(GenericClosedHierarchyData))]
        public async Task ClosedTypeInference_GenericHierarchy_ResolvesAndRoundTripsDerivedType(
            Type baseType,
            object value,
            string expectedJson,
            Type expectedDerivedType)
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            string json = await Serializer.SerializeWrapper(value, baseType, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);

            object roundtripped = await Serializer.DeserializeWrapper(json, baseType, options);
            Assert.IsType(expectedDerivedType, roundtripped);

            string roundtrippedJson = await Serializer.SerializeWrapper(roundtripped, baseType, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, roundtrippedJson);
        }

        public static IEnumerable<object[]> InvalidGenericClosedHierarchyData()
        {
            yield return new object[]
            {
                typeof(ClosedGroundMismatchBase<int, string>),
                new ClosedGroundMismatchFallback { Marker = "ground" },
            };
            yield return new object[]
            {
                typeof(ClosedRepeatedMismatchBase<int, string>),
                new ClosedRepeatedMismatchFallback { Marker = "repeated" },
            };
            yield return new object[]
            {
                typeof(ClosedConstraintViolationBase<string>),
                new ClosedConstraintViolationFallback { Marker = "constraint" },
            };
            yield return new object[]
            {
                typeof(ClosedUnspeakableBase<string>),
                new ClosedUnspeakableIdentityDerived<string>(),
            };
            yield return new object[]
            {
                typeof(ClosedNestedMismatchBase<ClosedNestedOuter<string>.NestedBox<int>>),
                new ClosedNestedMismatchFallback { Marker = "nested-mismatch" },
            };
            yield return new object[]
            {
                typeof(ClosedDeepJaggedMismatchBase<List<int[][]>>),
                new ClosedDeepJaggedMismatchFallback { Marker = "deep-mismatch" },
            };
            yield return new object[]
            {
                typeof(ClosedDuplicateArityBase<int, string>),
                new ClosedDuplicateArityDerived<int, string>(),
            };
        }

        [Theory]
        [MemberData(nameof(InvalidGenericClosedHierarchyData))]
        public async Task ClosedTypeInference_UnresolvableGenericDerivedType_ThrowsInvalidOperationException(
            Type baseType,
            object value)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Serializer.SerializeWrapper(value, baseType, ClosedTypeInferenceOptions));
        }

        [Fact]
        public async Task ClosedTypeInference_ConstructedGenericSiblingNotAssignable_ThrowsInvalidOperationException()
        {
            ClosedConcreteMismatchBase<string> value = new ClosedConcreteMismatchStringDerived();
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Serializer.SerializeWrapper(value, ClosedTypeInferenceOptions));
        }

        [Fact]
        public async Task ClosedTypeInference_DuplicateDiscriminator_ThrowsInvalidOperationException()
        {
            ClosedCollisionBase value = new ClosedCollisionHolderA.Node();
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Serializer.SerializeWrapper(value, ClosedTypeInferenceOptions));
            Assert.Contains(nameof(ClosedCollisionHolderA.Node), exception.Message);
        }

        [Fact]
        public async Task ClosedTypeInference_DuplicateGenericNameAcrossArities_ThrowsInvalidOperationException()
        {
            ClosedDuplicateArityBase<int, int> value = new ClosedDuplicateArityDerived<int>();
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Serializer.SerializeWrapper(value, ClosedTypeInferenceOptions));
            Assert.Contains(nameof(ClosedDuplicateArityDerived<int>), exception.Message);
        }

        public static IEnumerable<object[]> InaccessibleClosedHierarchyData()
        {
            yield return new object[]
            {
                typeof(ClosedAccessBase),
                new ClosedAccessiblePublicDerived(),
            };
            yield return new object[]
            {
                typeof(ClosedNestedAccessContainer.Base),
                new ClosedNestedAccessContainer.KeptDerived(),
            };
            yield return new object[]
            {
                typeof(ClosedProtectedAccessBase),
                new ClosedProtectedAccessibleDerived(),
            };
        }

        [Theory]
        [MemberData(nameof(InaccessibleClosedHierarchyData))]
        public async Task ClosedTypeInference_InaccessibleDerivedType(Type baseType, object value)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Serializer.SerializeWrapper(value, baseType, ClosedTypeInferenceOptions));
        }

        [Fact]
        public async Task ClosedTypeInference_ExplicitJsonDerivedType_SuppressesInference()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            Assert.Equal(
                new[] { "customA", "customB" },
                GetInferredDiscriminators(options, typeof(ClosedExplicitBase)));

            ClosedExplicitBase value = new ClosedExplicitA { BaseValue = "base", DerivedValue = 42 };
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual(
                """{"$type":"customA","DerivedValue":42,"BaseValue":"base"}""",
                json);

            ClosedExplicitBase roundtripped =
                await Serializer.DeserializeWrapper<ClosedExplicitBase>(json, options);
            ClosedExplicitA result = Assert.IsType<ClosedExplicitA>(roundtripped);
            Assert.Equal("base", result.BaseValue);
            Assert.Equal(42, result.DerivedValue);
        }

        [Fact]
        public async Task ClosedTypeInference_JsonPolymorphicAttribute_HonorsCustomDiscriminatorName()
        {
            ClosedCustomDiscriminatorBase value = new ClosedCustomDiscriminatorDerived
            {
                BaseValue = "base",
                DerivedValue = 42,
            };
            string json = await Serializer.SerializeWrapper(value, ClosedTypeInferenceOptions);
            JsonTestHelper.AssertJsonEqual(
                """{"$kind":"ClosedCustomDiscriminatorDerived","DerivedValue":42,"BaseValue":"base"}""",
                json);

            ClosedCustomDiscriminatorBase roundtripped =
                await Serializer.DeserializeWrapper<ClosedCustomDiscriminatorBase>(
                    json,
                    ClosedTypeInferenceOptions);
            ClosedCustomDiscriminatorDerived result =
                Assert.IsType<ClosedCustomDiscriminatorDerived>(roundtripped);
            Assert.Equal("base", result.BaseValue);
            Assert.Equal(42, result.DerivedValue);
        }
    }

    public closed class ClosedShape
    {
        public string? Name { get; set; }
    }
    public sealed class ClosedCircle : ClosedShape
    {
        public int Radius { get; set; }
    }
    public sealed class ClosedSquare : ClosedShape
    {
        public int SideLength { get; set; }
    }
    public sealed class ClosedTriangle : ClosedShape
    {
        public int BaseLength { get; set; }
        public int Height { get; set; }
    }

    public closed class ClosedPayload
    {
        public string? Id { get; set; }
    }
    public sealed class ClosedTextPayload : ClosedPayload { public string? Text { get; set; } }
    public sealed class ClosedNumberPayload : ClosedPayload { public int Number { get; set; } }

    public closed class ClosedEmptyBase;

    public abstract class PlainAbstractBase;
    public sealed class PlainAbstractDerived : PlainAbstractBase;

    public closed class ClosedContainer<T>
    {
        public T? BaseValue { get; set; }
    }
    public sealed class ClosedBox<T> : ClosedContainer<T> { public T? Value { get; set; } }
    public sealed class ClosedBag<T> : ClosedContainer<T> { public List<T>? Items { get; set; } }

    public closed class ClosedPair<TKey, TValue>
    {
        public TKey? BaseKey { get; set; }
        public TValue? BaseValue { get; set; }
    }
    public sealed class ClosedEntry<TKey, TValue> : ClosedPair<TKey, TValue>
    {
        public TKey? Key { get; set; }
        public TValue? Value { get; set; }
    }

    public closed class ClosedWrappedBase<T>
    {
        public T? BaseValue { get; set; }
    }
    public sealed class ClosedWrappedDerived<T> : ClosedWrappedBase<List<T>>
    {
        public List<T>? Data { get; set; }
    }

    public closed class ClosedArrayBase<T>
    {
        public T? BaseValue { get; set; }
    }
    public sealed class ClosedArrayDerived<T> : ClosedArrayBase<T[]>
    {
        public T[]? Values { get; set; }
    }

    public closed class ClosedReorderedBase<T1, T2>
    {
        public T1? BaseFirst { get; set; }
        public T2? BaseSecond { get; set; }
    }
    public sealed class ClosedReorderedDerived<T1, T2> : ClosedReorderedBase<T2, T1>
    {
        public T1? Left { get; set; }
        public T2? Right { get; set; }
    }

    public closed class ClosedPartialBase<T1, T2>
    {
        public T1? BaseFirst { get; set; }
        public T2? BaseSecond { get; set; }
    }
    public sealed class ClosedPartialDerived<T> : ClosedPartialBase<T, int>
    {
        public T? Value { get; set; }
    }

    public closed class ClosedKvpBase<T>
    {
        public T? BaseValue { get; set; }
    }
    public sealed class ClosedKvpDerived<T> : ClosedKvpBase<KeyValuePair<string, T>>
    {
        public KeyValuePair<string, T> Pair { get; set; }
    }

    public closed class ClosedTupleBase<T>
    {
        public string? BaseMarker { get; set; }
    }
    public sealed class ClosedTupleDerived<T1, T2> : ClosedTupleBase<(T1, T2)>
    {
        public string? Label { get; set; }
    }

    public sealed class ClosedNestedOuter<TOuter>
    {
        public sealed class NestedBox<TInner>;
    }

    public closed class ClosedNestedArgBase<T>
    {
        public string? BaseMarker { get; set; }
    }
    public sealed class ClosedNestedArgDerived<T> : ClosedNestedArgBase<ClosedNestedOuter<T>.NestedBox<int>>
    {
        public string? Marker { get; set; }
    }

    public closed class ClosedConstrainedBase<T>
    {
        public T? BaseValue { get; set; }
    }
    public sealed class ClosedConstrainedDerived<T> : ClosedConstrainedBase<T>
        where T : IEnumerable<object>
    {
        public T? Items { get; set; }
    }

    public closed class ClosedNestedDerivedBase<T>
    {
        public T? BaseValue { get; set; }

        public sealed class Derived : ClosedNestedDerivedBase<T>
        {
            public T? Value { get; set; }
        }
    }

    public closed class ClosedMixedBase<T>
    {
        public T? BaseValue { get; set; }
    }
    public sealed class ClosedMixedOpenDerived<T> : ClosedMixedBase<T>
    {
        public string? Marker { get; set; }
    }
    public sealed class ClosedMixedFixedDerived : ClosedMixedBase<int>
    {
        public string? Marker { get; set; }
    }

    public closed class ClosedGroundMismatchBase<T1, T2>;
    public sealed class ClosedGroundMismatchDerived<T> : ClosedGroundMismatchBase<T, int>;
    public sealed class ClosedGroundMismatchFallback : ClosedGroundMismatchBase<int, string>
    {
        public string? Marker { get; set; }
    }

    public closed class ClosedRepeatedBase<T1, T2>
    {
        public T1? First { get; set; }
        public T2? Second { get; set; }
    }
    public sealed class ClosedRepeatedDerived<T> : ClosedRepeatedBase<T, T>
    {
        public string? Marker { get; set; }
    }
    public closed class ClosedRepeatedMismatchBase<T1, T2>;
    public sealed class ClosedRepeatedMismatchDerived<T> : ClosedRepeatedMismatchBase<T, T>;
    public sealed class ClosedRepeatedMismatchFallback : ClosedRepeatedMismatchBase<int, string>
    {
        public string? Marker { get; set; }
    }

    public closed class ClosedConstraintViolationBase<T>;
    public sealed class ClosedConstraintViolationDerived<T> : ClosedConstraintViolationBase<T>
        where T : struct;
    public sealed class ClosedConstraintViolationFallback : ClosedConstraintViolationBase<string>
    {
        public string? Marker { get; set; }
    }

    public closed class ClosedNestedMismatchBase<T>;
    public sealed class ClosedNestedMismatchDerived<T> :
        ClosedNestedMismatchBase<ClosedNestedOuter<int>.NestedBox<T>>;
    public sealed class ClosedNestedMismatchFallback :
        ClosedNestedMismatchBase<ClosedNestedOuter<string>.NestedBox<int>>
    {
        public string? Marker { get; set; }
    }

    public closed class ClosedDeepJaggedBase<T>
    {
        public string? BaseMarker { get; set; }
    }
    public sealed class ClosedDeepJaggedDerived<T> : ClosedDeepJaggedBase<List<T[][][]>>
    {
        public string? Marker { get; set; }
    }

    public closed class ClosedDeepJaggedMismatchBase<T>;
    public sealed class ClosedDeepJaggedMismatchDerived<T> : ClosedDeepJaggedMismatchBase<List<T[][][]>>;
    public sealed class ClosedDeepJaggedMismatchFallback : ClosedDeepJaggedMismatchBase<List<int[][]>>
    {
        public string? Marker { get; set; }
    }

    public closed class ClosedDuplicateArityBase<T1, T2>;
    public sealed class ClosedDuplicateArityDerived<T> : ClosedDuplicateArityBase<T, T>;
    public sealed class ClosedDuplicateArityDerived<T1, T2> : ClosedDuplicateArityBase<T1, T2>;

    public closed class ClosedUnspeakableBase<T>;
    public sealed class ClosedUnspeakableIdentityDerived<T> : ClosedUnspeakableBase<T>;
    public sealed class ClosedUnspeakableArrayDerived<T> : ClosedUnspeakableBase<T[]>;

    public closed class ClosedConcreteMismatchBase<T>;
    public sealed class ClosedConcreteMismatchIntDerived : ClosedConcreteMismatchBase<int>;
    public sealed class ClosedConcreteMismatchStringDerived : ClosedConcreteMismatchBase<string>;

    public closed class ClosedCollisionBase;
    public static class ClosedCollisionHolderA { public sealed class Node : ClosedCollisionBase; }
    public static class ClosedCollisionHolderB { public sealed class Node : ClosedCollisionBase; }

    public closed class ClosedAccessBase;
    public sealed class ClosedAccessiblePublicDerived : ClosedAccessBase;
    internal sealed class ClosedAccessInternalDerived : ClosedAccessBase;

    public closed class ClosedProtectedAccessBase;
    public sealed class ClosedProtectedAccessibleDerived : ClosedProtectedAccessBase;

    public class ClosedProtectedAccessContainer
    {
        protected sealed class HiddenDerived : ClosedProtectedAccessBase;
    }

    public class ClosedNestedAccessContainer
    {
        protected internal closed class Base;
        protected internal sealed class KeptDerived : Base;
        internal sealed class DroppedDerived : Base;
    }

    [JsonDerivedType(typeof(ClosedExplicitA), "customA")]
    [JsonDerivedType(typeof(ClosedExplicitB), "customB")]
    public closed class ClosedExplicitBase
    {
        public string? BaseValue { get; set; }
    }
    public sealed class ClosedExplicitA : ClosedExplicitBase
    {
        public int DerivedValue { get; set; }
    }
    public sealed class ClosedExplicitB : ClosedExplicitBase
    {
        public int DerivedValue { get; set; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    public closed class ClosedCustomDiscriminatorBase
    {
        public string? BaseValue { get; set; }
    }
    public sealed class ClosedCustomDiscriminatorDerived : ClosedCustomDiscriminatorBase
    {
        public int DerivedValue { get; set; }
    }

    public sealed class ClosedShapeHolder
    {
        public string? Name { get; set; }
        public ClosedShape? Shape { get; set; }
    }
}
