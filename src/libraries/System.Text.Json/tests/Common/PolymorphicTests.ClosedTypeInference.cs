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

        private static (Type DerivedType, string Discriminator)[] GetInferredDerivedTypes(JsonSerializerOptions options, Type baseType)
        {
            JsonTypeInfo typeInfo = options.GetTypeInfo(baseType);
            Assert.NotNull(typeInfo.PolymorphismOptions);
            return typeInfo.PolymorphismOptions.DerivedTypes
                .Select(derivedType => (derivedType.DerivedType, (string)derivedType.TypeDiscriminator!))
                .OrderBy(entry => entry.Item2, StringComparer.Ordinal)
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

        public static IEnumerable<object[]> NestedClosedHierarchyData()
        {
            yield return new object[]
            {
                new ClosedCat { Name = "Mittens", Indoor = true },
                """{"$type":"ClosedCat","Indoor":true,"Name":"Mittens"}""",
            };
            yield return new object[]
            {
                new ClosedLabrador { Name = "Rex", GoodBoy = true, Color = "black" },
                """{"$type":"ClosedLabrador","Color":"black","GoodBoy":true,"Name":"Rex"}""",
            };
            yield return new object[]
            {
                new ClosedCollie { Name = "Lassie", GoodBoy = true, Herding = true },
                """{"$type":"ClosedCollie","Herding":true,"GoodBoy":true,"Name":"Lassie"}""",
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
                [
                    (typeof(ClosedCircle), nameof(ClosedCircle)),
                    (typeof(ClosedSquare), nameof(ClosedSquare)),
                    (typeof(ClosedTriangle), nameof(ClosedTriangle)),
                ],
                GetInferredDerivedTypes(ClosedTypeInferenceOptions, typeof(ClosedShape)));
            Assert.Equal(
                [
                    (typeof(ClosedBag<int>), nameof(ClosedBag<int>)),
                    (typeof(ClosedBox<int>), nameof(ClosedBox<int>)),
                ],
                GetInferredDerivedTypes(ClosedTypeInferenceOptions, typeof(ClosedContainer<int>)));
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

        [Theory]
        [MemberData(nameof(NestedClosedHierarchyData))]
        public async Task ClosedTypeInference_NestedHierarchy_RoundTripsConcreteDescendants(
            ClosedPet value,
            string expectedJson)
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            Type expectedDerivedType = value.GetType();

            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual(expectedJson, json);

            ClosedPet roundtripped = await Serializer.DeserializeWrapper<ClosedPet>(json, options);
            Assert.IsType(expectedDerivedType, roundtripped);
        }

        [Fact]
        public async Task ClosedTypeInference_NestedHierarchy_UsesIndependentBaseContracts()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            Assert.Equal(
                [
                    (typeof(ClosedCat), nameof(ClosedCat)),
                    (typeof(ClosedCollie), nameof(ClosedCollie)),
                    (typeof(ClosedLabrador), nameof(ClosedLabrador)),
                ],
                GetInferredDerivedTypes(options, typeof(ClosedPet)));

            var labrador = new ClosedLabrador { Name = "Rex", GoodBoy = true, Color = "black" };
            string petJson = await Serializer.SerializeWrapper(labrador, typeof(ClosedPet), options);
            string dogJson = await Serializer.SerializeWrapper(labrador, typeof(ClosedDog), options);

            JsonTestHelper.AssertJsonEqual(
                """{"$type":"ClosedLabrador","Color":"black","GoodBoy":true,"Name":"Rex"}""",
                petJson);
            JsonTestHelper.AssertJsonEqual(
                """{"$dog":"lab","Color":"black","GoodBoy":true,"Name":"Rex"}""",
                dogJson);

            var collie = new ClosedCollie { Name = "Lassie", GoodBoy = true, Herding = true };
            await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.SerializeWrapper(collie, typeof(ClosedDog), options));
        }

        [Fact]
        public async Task ClosedTypeInference_NestedHierarchy_IgnoresIntermediateOptOutAndConverter()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            var value = new ClosedNestedConverterLeaf { Value = 42 };

            string rootJson =
                await Serializer.SerializeWrapper(value, typeof(ClosedNestedConverterRoot), options);

            JsonTestHelper.AssertJsonEqual(
                """{"$type":"ClosedNestedConverterLeaf","Value":42}""",
                rootJson);
        }

        [Fact]
        public void ClosedTypeInference_NestedHierarchyWithoutTerminalDerivedTypes_Throws()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(
                    value: null,
                    ClosedTypeInferenceOptions.GetTypeInfo(typeof(ClosedNestedEmptyRoot))));

            Assert.Contains(typeof(ClosedNestedEmptyRoot).ToString(), exception.Message);
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
        public async Task ClosedTypeInference_JsonPolymorphicAttribute_AppliesToAttributedHierarchyOnly()
        {
            JsonSerializerOptions options = Serializer.DefaultOptions;
            Assert.False(options.InferClosedTypePolymorphism);
            Assert.Equal(
                [(typeof(ClosedAttributeOptInDerived), nameof(ClosedAttributeOptInDerived))],
                GetInferredDerivedTypes(options, typeof(ClosedAttributeOptInBase)));

            ClosedAttributeOptInBase optedIn = new ClosedAttributeOptInDerived
            {
                BaseValue = "base",
                DerivedValue = 42,
            };
            string optedInJson = await Serializer.SerializeWrapper(optedIn, options);
            JsonTestHelper.AssertJsonEqual(
                """{"$kind":"ClosedAttributeOptInDerived","DerivedValue":42,"BaseValue":"base"}""",
                optedInJson);

            ClosedAttributeOptInBase roundtripped =
                await Serializer.DeserializeWrapper<ClosedAttributeOptInBase>(optedInJson, options);
            ClosedAttributeOptInDerived result = Assert.IsType<ClosedAttributeOptInDerived>(roundtripped);
            Assert.Equal("base", result.BaseValue);
            Assert.Equal(42, result.DerivedValue);

            Assert.Null(options.GetTypeInfo(typeof(ClosedAttributeOptOutBase)).PolymorphismOptions);
            ClosedAttributeOptOutBase optedOut = new ClosedAttributeOptOutDerived
            {
                BaseValue = "base",
                DerivedValue = 42,
            };
            string optedOutJson = await Serializer.SerializeWrapper(optedOut, options);
            JsonTestHelper.AssertJsonEqual("""{"BaseValue":"base"}""", optedOutJson);
        }

        [Fact]
        public void ClosedTypeInference_JsonPolymorphicAttribute_IsRedundantWithGlobalOption()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            Assert.True(options.InferClosedTypePolymorphism);
            Assert.Equal(
                [(typeof(ClosedAttributeOptInDerived), nameof(ClosedAttributeOptInDerived))],
                GetInferredDerivedTypes(options, typeof(ClosedAttributeOptInBase)));
        }

        [Fact]
        public void ClosedTypeInference_JsonPolymorphicAttribute_OnNonClosedTypeDoesNotInfer()
        {
            JsonSerializerOptions options = Serializer.DefaultOptions;

            // Derived types can never be inferred for a type that is not closed. The reflection resolver
            // reports that directly; source generation reports it as SYSLIB1243 at compile time and falls
            // back to the generic empty-registration failure at run time.
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => options.GetTypeInfo(typeof(NonClosedAttributeBase)));

            Assert.Contains(typeof(NonClosedAttributeBase).ToString(), exception.Message);

            if (!Serializer.IsSourceGeneratedSerializer)
            {
                Assert.Contains(
                    nameof(JsonPolymorphicAttribute.InferClosedTypePolymorphism),
                    exception.Message);
            }
        }

        [Fact]
        public void ClosedTypeInference_JsonPolymorphicAttribute_OnNonClosedTypeWithExplicitDerivedTypes_Throws()
        {
            // Explicit JsonDerivedTypeAttribute registrations do not make the opt-in meaningful: no derived
            // type can ever be inferred for a type that is not closed, so the declaration is rejected rather
            // than left to silently behave like a plain JsonPolymorphicAttribute. Source generation rejects
            // the equivalent declaration at compile time with SYSLIB1243, so this fixture is unregistered
            // there and the assertion is reflection-only.
            if (Serializer.IsSourceGeneratedSerializer)
            {
                return;
            }

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => Serializer.DefaultOptions.GetTypeInfo(typeof(NonClosedExplicitBase)));

            Assert.Contains(typeof(NonClosedExplicitBase).ToString(), exception.Message);
            Assert.Contains(
                nameof(JsonPolymorphicAttribute.InferClosedTypePolymorphism),
                exception.Message);
        }

        [Theory]
        [InlineData(typeof(ClosedEmptyAttributeOptInBase))]
        [InlineData(typeof(ClosedEmptyOptOutWithCustomDiscriminatorBase))]
        [InlineData(typeof(ClosedEmptyOptOutWithIgnoreUnrecognizedDiscriminatorsBase))]
        [InlineData(typeof(ClosedEmptyOptOutWithTypeClassifierBase))]
        [InlineData(typeof(ClosedEmptyOptOutWithUnknownDerivedTypeHandlingBase))]
        [InlineData(typeof(ClosedEmptyPolymorphicBase))]
        public void ClosedTypeInference_JsonPolymorphicAttribute_OnClosedTypeWithoutDerivedTypes_ThrowsMissingDerivedTypes(Type type)
        {
            // Enabling inference does not change what an empty JsonPolymorphicAttribute registration means:
            // a closed type declaring no derived types fails the same way any other polymorphic declaration
            // without derived types has failed since .NET 7, and the message must not blame the opt-in.
            JsonSerializerOptions[] optionsToTest = [Serializer.DefaultOptions, ClosedTypeInferenceOptions];

            foreach (JsonSerializerOptions options in optionsToTest)
            {
                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => JsonSerializer.Serialize(value: null, options.GetTypeInfo(type)));

                Assert.Contains(type.ToString(), exception.Message);
                Assert.DoesNotContain(
                    nameof(JsonPolymorphicAttribute.InferClosedTypePolymorphism),
                    exception.Message);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ClosedTypeInference_JsonPolymorphicAttribute_ExplicitFalseSuppressesInference(bool globalOptIn)
        {
            JsonSerializerOptions options =
                globalOptIn ? ClosedTypeInferenceOptions : Serializer.DefaultOptions;
            Assert.Equal(globalOptIn, options.InferClosedTypePolymorphism);

            // A value declared on the type overrides the global setting, so the hierarchy stays
            // non-polymorphic even when inference is enabled globally.
            Assert.Null(options.GetTypeInfo(typeof(ClosedExplicitOptOutBase)).PolymorphismOptions);

            ClosedExplicitOptOutBase value = new ClosedExplicitOptOutDerived
            {
                BaseValue = "base",
                DerivedValue = 42,
            };
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""{"BaseValue":"base"}""", json);
        }

        [Fact]
        public void ClosedTypeInference_EmptyDerivedTypes_IsInert()
        {
            // A closed class with no descendants is a valid C# declaration. With no polymorphism attributes
            // of its own it stays non-polymorphic, whether or not inference is enabled.
            Assert.Null(Serializer.DefaultOptions.GetTypeInfo(typeof(ClosedEmptyBase)).PolymorphismOptions);
            Assert.Null(ClosedTypeInferenceOptions.GetTypeInfo(typeof(ClosedEmptyBase)).PolymorphismOptions);
        }

        [Fact]
        public async Task ClosedTypeInference_NonClosedBranch_RemainsAnInferenceBoundary()
        {
            JsonSerializerOptions options = ClosedTypeInferenceOptions;
            Assert.Equal(
                [(typeof(ClosedOpenBranch), nameof(ClosedOpenBranch))],
                GetInferredDerivedTypes(options, typeof(ClosedOpenRoot)));

            await Assert.ThrowsAsync<NotSupportedException>(
                () => Serializer.SerializeWrapper(
                    new ClosedOpenLeaf(),
                    typeof(ClosedOpenRoot),
                    options));
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
                    BaseValue = [4, 5],
                    Values = [1, 2, 3],
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

        [Fact]
        public async Task ClosedTypeInference_NestedGenericHierarchy_ResolvesAndRoundTripsDerivedType()
        {
            Type baseType = typeof(ClosedNestedGenericRoot<List<int[]>>);
            var value = new ClosedNestedGenericLeaf<int>
            {
                BaseValue = [[1, 2]],
                MiddleValue = [3, 4],
                LeafValue = 5,
            };
            const string ExpectedJson =
                """{"$type":"ClosedNestedGenericLeaf","LeafValue":5,"MiddleValue":[3,4],"BaseValue":[[1,2]]}""";

            string json = await Serializer.SerializeWrapper(value, baseType, ClosedTypeInferenceOptions);
            JsonTestHelper.AssertJsonEqual(ExpectedJson, json);

            object roundtripped =
                await Serializer.DeserializeWrapper(json, baseType, ClosedTypeInferenceOptions);
            Assert.IsType<ClosedNestedGenericLeaf<int>>(roundtripped);
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
        public async Task ClosedTypeInference_NestedDuplicateDiscriminator_ThrowsInvalidOperationException()
        {
            ClosedNestedCollisionBase value = new ClosedNestedCollisionHolderA.Node();
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Serializer.SerializeWrapper(value, ClosedTypeInferenceOptions));
            Assert.Contains(nameof(ClosedNestedCollisionHolderA.Node), exception.Message);
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
        public async Task ClosedTypeInference_NestedInaccessibleDerivedType()
        {
            ClosedNestedAccessBase value = new ClosedNestedAccessDerived();
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => Serializer.SerializeWrapper(value, ClosedTypeInferenceOptions));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task ClosedTypeInference_ExplicitJsonDerivedType_SuppressesInference(bool attributeOptIn)
        {
            JsonSerializerOptions options =
                attributeOptIn ? Serializer.DefaultOptions : ClosedTypeInferenceOptions;
            Type baseType =
                attributeOptIn ? typeof(ClosedAttributeExplicitBase) : typeof(ClosedExplicitBase);
            object value = attributeOptIn
                ? new ClosedAttributeExplicitA { BaseValue = "base", DerivedValue = 42 }
                : new ClosedExplicitA { BaseValue = "base", DerivedValue = 42 };
            Type expectedDerivedType =
                attributeOptIn ? typeof(ClosedAttributeExplicitA) : typeof(ClosedExplicitA);

            (Type, string)[] expectedDerivedTypes = attributeOptIn
                ? [(typeof(ClosedAttributeExplicitA), "customA"), (typeof(ClosedAttributeExplicitB), "customB")]
                : [(typeof(ClosedExplicitA), "customA"), (typeof(ClosedExplicitB), "customB")];

            Assert.Equal(expectedDerivedTypes, GetInferredDerivedTypes(options, baseType));

            string json = await Serializer.SerializeWrapper(value, baseType, options);
            JsonTestHelper.AssertJsonEqual(
                """{"$type":"customA","DerivedValue":42,"BaseValue":"base"}""",
                json);

            object roundtripped = await Serializer.DeserializeWrapper(json, baseType, options);
            Assert.IsType(expectedDerivedType, roundtripped);
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

    public closed class ClosedPet
    {
        public string? Name { get; set; }
    }

    public sealed class ClosedCat : ClosedPet
    {
        public bool Indoor { get; set; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$dog")]
    [JsonDerivedType(typeof(ClosedLabrador), "lab")]
    public closed class ClosedDog : ClosedPet
    {
        public bool GoodBoy { get; set; }
    }

    public sealed class ClosedLabrador : ClosedDog
    {
        public string? Color { get; set; }
    }

    public sealed class ClosedCollie : ClosedDog
    {
        public bool Herding { get; set; }
    }

    public closed class ClosedNestedEmptyRoot;

    public closed class ClosedNestedEmptyMiddle : ClosedNestedEmptyRoot;

    public closed class ClosedNestedConverterRoot;

    [JsonPolymorphic(InferClosedTypePolymorphism = false)]
    [JsonConverter(typeof(ClosedNestedConverter))]
    public closed class ClosedNestedConverterMiddle : ClosedNestedConverterRoot;

    public sealed class ClosedNestedConverterLeaf : ClosedNestedConverterMiddle
    {
        public int Value { get; set; }
    }

    public sealed class ClosedNestedConverter : JsonConverter<ClosedNestedConverterMiddle>
    {
        public override ClosedNestedConverterMiddle? Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) => throw new NotSupportedException();

        public override void Write(
            Utf8JsonWriter writer,
            ClosedNestedConverterMiddle value,
            JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteBoolean("FromIntermediateConverter", true);
            writer.WriteEndObject();
        }
    }

    [JsonPolymorphic(
        InferClosedTypePolymorphism = true,
        TypeDiscriminatorPropertyName = "$kind")]
    public closed class ClosedAttributeOptInBase
    {
        public string? BaseValue { get; set; }
    }
    public sealed class ClosedAttributeOptInDerived : ClosedAttributeOptInBase
    {
        public int DerivedValue { get; set; }
    }

    public closed class ClosedAttributeOptOutBase
    {
        public string? BaseValue { get; set; }
    }
    public sealed class ClosedAttributeOptOutDerived : ClosedAttributeOptOutBase
    {
        public int DerivedValue { get; set; }
    }

    [JsonPolymorphic(InferClosedTypePolymorphism = false)]
    public closed class ClosedExplicitOptOutBase
    {
        public string? BaseValue { get; set; }
    }
    public sealed class ClosedExplicitOptOutDerived : ClosedExplicitOptOutBase
    {
        public int DerivedValue { get; set; }
    }

    // The opt-in is intentionally applied to a non-closed type, which the source generator reports.
#pragma warning disable SYSLIB1243
    [JsonPolymorphic(InferClosedTypePolymorphism = true)]
    public abstract class NonClosedAttributeBase;
#pragma warning restore SYSLIB1243
    public sealed class NonClosedAttributeDerived : NonClosedAttributeBase;

    // Deliberately not registered with any JsonSerializerContext: source generation rejects this
    // declaration at compile time, so only the reflection resolver can observe its runtime behavior.
    [JsonPolymorphic(InferClosedTypePolymorphism = true)]
    [JsonDerivedType(typeof(NonClosedExplicitDerived), "derived")]
    public abstract class NonClosedExplicitBase
    {
        public string? BaseValue { get; set; }
    }
    public sealed class NonClosedExplicitDerived : NonClosedExplicitBase
    {
        public int DerivedValue { get; set; }
    }

    public closed class ClosedPayload
    {
        public string? Id { get; set; }
    }
    public sealed class ClosedTextPayload : ClosedPayload { public string? Text { get; set; } }
    public sealed class ClosedNumberPayload : ClosedPayload { public int Number { get; set; } }

    public closed class ClosedEmptyBase;

    public closed class ClosedOpenRoot;

    public class ClosedOpenBranch : ClosedOpenRoot;

    public sealed class ClosedOpenLeaf : ClosedOpenBranch;

    [JsonPolymorphic(InferClosedTypePolymorphism = true)]
    public closed class ClosedEmptyAttributeOptInBase
    {
        public string? BaseValue { get; set; }
    }

    [JsonPolymorphic(InferClosedTypePolymorphism = false, TypeDiscriminatorPropertyName = "$kind")]
    public closed class ClosedEmptyOptOutWithCustomDiscriminatorBase
    {
        public string? BaseValue { get; set; }
    }

    [JsonPolymorphic(InferClosedTypePolymorphism = false, IgnoreUnrecognizedTypeDiscriminators = true)]
    public closed class ClosedEmptyOptOutWithIgnoreUnrecognizedDiscriminatorsBase
    {
        public string? BaseValue { get; set; }
    }

    [JsonPolymorphic(InferClosedTypePolymorphism = false, TypeClassifier = typeof(ClosedEmptyOptOutTypeClassifierFactory))]
    public closed class ClosedEmptyOptOutWithTypeClassifierBase
    {
        public string? BaseValue { get; set; }
    }

    public sealed class ClosedEmptyOptOutTypeClassifierFactory : JsonTypeClassifierFactory
    {
        public override bool CanClassify(JsonTypeClassifierContext context) => true;

        public override JsonTypeClassifier CreateJsonClassifier(
            JsonTypeClassifierContext context,
            JsonSerializerOptions options) => (ref Utf8JsonReader reader) => null;
    }

    [JsonPolymorphic(InferClosedTypePolymorphism = false, UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
    public closed class ClosedEmptyOptOutWithUnknownDerivedTypeHandlingBase
    {
        public string? BaseValue { get; set; }
    }

    [JsonPolymorphic]
    public closed class ClosedEmptyPolymorphicBase
    {
        public string? BaseValue { get; set; }
    }

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

    public closed class ClosedNestedGenericRoot<T>
    {
        public T? BaseValue { get; set; }
    }

    public closed class ClosedNestedGenericMiddle<T> : ClosedNestedGenericRoot<List<T>>
    {
        public T? MiddleValue { get; set; }
    }

    public sealed class ClosedNestedGenericLeaf<T> : ClosedNestedGenericMiddle<T[]>
    {
        public T? LeafValue { get; set; }
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

    public closed class ClosedNestedCollisionBase;

    public static class ClosedNestedCollisionHolderA { public sealed class Node : ClosedNestedCollisionBase; }

    public closed class ClosedNestedCollisionMiddle : ClosedNestedCollisionBase;

    public static class ClosedNestedCollisionHolderB { public sealed class Node : ClosedNestedCollisionMiddle; }

    public closed class ClosedAccessBase;
    public sealed class ClosedAccessiblePublicDerived : ClosedAccessBase;
    internal sealed class ClosedAccessInternalDerived : ClosedAccessBase;

    public closed class ClosedNestedAccessBase;

    public closed class ClosedNestedAccessMiddle : ClosedNestedAccessBase;

    internal sealed class ClosedNestedAccessDerived : ClosedNestedAccessMiddle;

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

    // Explicit registrations replace inference, which the source generator reports.
#pragma warning disable SYSLIB1244
    [JsonPolymorphic(InferClosedTypePolymorphism = true)]
    [JsonDerivedType(typeof(ClosedAttributeExplicitA), "customA")]
    [JsonDerivedType(typeof(ClosedAttributeExplicitB), "customB")]
    public closed class ClosedAttributeExplicitBase
    {
        public string? BaseValue { get; set; }
    }
#pragma warning restore SYSLIB1244
    public sealed class ClosedAttributeExplicitA : ClosedAttributeExplicitBase
    {
        public int DerivedValue { get; set; }
    }
    public sealed class ClosedAttributeExplicitB : ClosedAttributeExplicitBase
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
