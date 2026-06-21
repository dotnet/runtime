// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class StructuralJsonTypeClassifierTests(JsonSerializerWrapper serializerUnderTest) : SerializerTests(serializerUnderTest)
    {
        private static readonly JsonSerializerOptions s_options = new()
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        private static readonly JsonSerializerOptions s_numberFromStringOptions = new()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };

        [Fact]
        public async Task StructuralClassifier_DistinguishesObjectProperties()
        {
            PetUnion? dog = await Serializer.DeserializeWrapper<PetUnion>("""{"Name":"Rex","Breed":"Labrador"}""", s_options);
            Assert.NotNull(dog);
            Dog dogValue = Assert.IsType<Dog>(GetUnionValue(dog));
            Assert.Equal("Rex", dogValue.Name);
            Assert.Equal("Labrador", dogValue.Breed);

            PetUnion? cat = await Serializer.DeserializeWrapper<PetUnion>("""{"Name":"Misty","Lives":9}""", s_options);
            Assert.NotNull(cat);
            Cat catValue = Assert.IsType<Cat>(GetUnionValue(cat));
            Assert.Equal("Misty", catValue.Name);
            Assert.Equal(9, catValue.Lives);
        }

        [Fact]
        public async Task StructuralClassifier_DistinguishesStringPatterns()
        {
            TemporalUnion? date = await Serializer.DeserializeWrapper<TemporalUnion>("\"2026-05-11T18:00:00Z\"", s_options);
            Assert.NotNull(date);
            Assert.IsType<DateTime>(GetUnionValue(date));

            TemporalUnion? duration = await Serializer.DeserializeWrapper<TemporalUnion>("\"01:02:03\"", s_options);
            Assert.NotNull(duration);
            Assert.Equal(TimeSpan.FromSeconds(3723), Assert.IsType<TimeSpan>(GetUnionValue(duration)));

            TemporalUnion? guid = await Serializer.DeserializeWrapper<TemporalUnion>("\"0f8fad5b-d9cb-469f-a165-70867728950e\"", s_options);
            Assert.NotNull(guid);
            Assert.Equal(new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), Assert.IsType<Guid>(GetUnionValue(guid)));
        }

        [Fact]
        public async Task StructuralClassifier_SamplesCollectionElementShapes()
        {
            ListUnion? numbers = await Serializer.DeserializeWrapper<ListUnion>("[1,2,3]", s_options);
            Assert.NotNull(numbers);
            Assert.Equal([1, 2, 3], Assert.IsType<List<int>>(GetUnionValue(numbers)));

            ListUnion? strings = await Serializer.DeserializeWrapper<ListUnion>("""["one","two"]""", s_options);
            Assert.NotNull(strings);
            Assert.Equal(["one", "two"], Assert.IsType<List<string>>(GetUnionValue(strings)));
        }

        [Fact]
        public async Task StructuralClassifier_SamplesDictionaryValueShapes()
        {
            DictionaryUnion? numbers = await Serializer.DeserializeWrapper<DictionaryUnion>("""{"one":1,"two":2}""", s_options);
            Assert.NotNull(numbers);
            Assert.Equal(2, Assert.IsType<Dictionary<string, int>>(GetUnionValue(numbers))["two"]);

            DictionaryUnion? strings = await Serializer.DeserializeWrapper<DictionaryUnion>("""{"one":"uno","two":"dos"}""", s_options);
            Assert.NotNull(strings);
            Assert.Equal("dos", Assert.IsType<Dictionary<string, string>>(GetUnionValue(strings))["two"]);
        }

        [Fact]
        public async Task StructuralClassifier_RecursesIntoNestedGenericShapes()
        {
            BatchUnion? temperatures = await Serializer.DeserializeWrapper<BatchUnion>("""{"Source":"sensor","Items":[{"Celsius":21.5}]}""", s_options);
            Assert.NotNull(temperatures);
            Batch<TemperatureReading> temperatureBatch = Assert.IsType<Batch<TemperatureReading>>(GetUnionValue(temperatures));
            Assert.Equal(21.5, temperatureBatch.Items![0].Celsius);

            BatchUnion? statuses = await Serializer.DeserializeWrapper<BatchUnion>("""{"Source":"sensor","Items":[{"IsOnline":true}]}""", s_options);
            Assert.NotNull(statuses);
            Batch<StatusReading> statusBatch = Assert.IsType<Batch<StatusReading>>(GetUnionValue(statuses));
            Assert.True(statusBatch.Items![0].IsOnline);
        }

        [Fact]
        public async Task StructuralClassifier_DistinguishesArrayFromDictionary()
        {
            ArrayOrDictionaryUnion? array = await Serializer.DeserializeWrapper<ArrayOrDictionaryUnion>("[1,2,3]", s_options);
            Assert.NotNull(array);
            Assert.Equal([1, 2, 3], Assert.IsType<List<int>>(GetUnionValue(array)));

            ArrayOrDictionaryUnion? dictionary = await Serializer.DeserializeWrapper<ArrayOrDictionaryUnion>("""{"one":1,"two":2}""", s_options);
            Assert.NotNull(dictionary);
            Assert.Equal(2, Assert.IsType<Dictionary<string, int>>(GetUnionValue(dictionary))["two"]);
        }

        [Fact]
        public async Task StructuralClassifier_PrefersObjectShapeOverDictionaryWhenPropertiesMatch()
        {
            ObjectOrDictionaryUnion? result = await Serializer.DeserializeWrapper<ObjectOrDictionaryUnion>("""{"X":1,"Y":2}""", s_options);
            Assert.NotNull(result);
            Point point = Assert.IsType<Point>(GetUnionValue(result));
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
        }

        [Fact]
        public async Task StructuralClassifier_UsesJsonTypeInfoPropertyNames()
        {
            RenamedPropertyUnion? result = await Serializer.DeserializeWrapper<RenamedPropertyUnion>("""{"kind":"special"}""", s_options);
            Assert.NotNull(result);
            RenamedPropertyCase value = Assert.IsType<RenamedPropertyCase>(GetUnionValue(result));
            Assert.Equal("special", value.Kind);
        }

        [Fact]
        public async Task StructuralClassifier_UsesCaseInsensitivePropertyNames()
        {
            PetUnion? dog = await Serializer.DeserializeWrapper<PetUnion>("""{"name":"Rex","breed":"Labrador"}""", s_caseInsensitiveOptions);
            Assert.NotNull(dog);
            Dog dogValue = Assert.IsType<Dog>(GetUnionValue(dog));
            Assert.Equal("Rex", dogValue.Name);
            Assert.Equal("Labrador", dogValue.Breed);
        }

        [Fact]
        public async Task StructuralClassifier_UsesNumberHandlingMetadata()
        {
            NumberHandlingUnion? result = await Serializer.DeserializeWrapper<NumberHandlingUnion>("\"42\"", s_numberFromStringOptions);
            Assert.NotNull(result);
            Assert.Equal(42, Assert.IsType<int>(GetUnionValue(result)));
        }

        [Theory]
        [InlineData("42", typeof(NumericUnion))]
        [InlineData("\"2026-05-11T18:00:00Z\"", typeof(DateOrDateTimeOffsetUnion))]
        [InlineData("""{"Name":"Misty","Age":5}""", typeof(IdenticalPetUnion))]
        [InlineData("[]", typeof(ListUnion))]
        [InlineData("true", typeof(PetUnion))]
        public async Task StructuralClassifier_AmbiguousOrUnsupportedShapesThrow(string json, Type unionType)
        {
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper(json, unionType, s_options));
        }

        [Fact]
        public async Task StructuralClassifier_WorksForPolymorphicDerivedTypeMetadata()
        {
            Shape? circle = await Serializer.DeserializeWrapper<Shape>("""{"Color":"red","Radius":2.5}""", s_options);
            Assert.NotNull(circle);
            Assert.Equal(2.5, Assert.IsType<Circle>(circle).Radius);

            Shape? rectangle = await Serializer.DeserializeWrapper<Shape>("""{"Color":"blue","Width":3,"Height":4}""", s_options);
            Assert.NotNull(rectangle);
            Assert.Equal(4, Assert.IsType<Rectangle>(rectangle).Height);
        }

        private static object? GetUnionValue<TUnion>(TUnion? union)
            where TUnion : struct, IUnion
        {
            Assert.True(union.HasValue);

            return union.GetValueOrDefault().Value;
        }

        public sealed class StructuralJsonTypeClassifierFactory : JsonTypeClassifierFactory
        {
            private const int MaxDepth = 8;

            public override bool CanClassify(JsonTypeClassifierContext context) => true; // Works for any polymorphic or union type.
            public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
            {
                if (context is null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (options is null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                Candidate[] candidates = context.Kind switch
                {
                    JsonTypeClassifierKind.Union => CreateUnionCandidates(context.UnionCases, options),
                    JsonTypeClassifierKind.PolymorphicType => CreatePolymorphicCandidates(context.DerivedTypes, options),
                    _ => []
                };

                return (ref Utf8JsonReader reader) => Classify(candidates, ref reader);
            }

            private static Candidate[] CreateUnionCandidates(IReadOnlyList<JsonUnionCaseInfo> unionCases, JsonSerializerOptions options)
            {
                var candidates = new Candidate[unionCases.Count];
                var cache = new Dictionary<Type, Shape>();
                for (int i = 0; i < unionCases.Count; i++)
                {
                    Type type = unionCases[i].CaseType;
                    candidates[i] = new Candidate(type, BuildShape(type, options, cache, numberHandling: null, depth: 0));
                }

                return candidates;
            }

            private static Candidate[] CreatePolymorphicCandidates(IReadOnlyList<JsonDerivedType> derivedTypes, JsonSerializerOptions options)
            {
                var candidates = new Candidate[derivedTypes.Count];
                var cache = new Dictionary<Type, Shape>();
                for (int i = 0; i < derivedTypes.Count; i++)
                {
                    Type type = derivedTypes[i].DerivedType;
                    candidates[i] = new Candidate(type, BuildShape(type, options, cache, numberHandling: null, depth: 0));
                }

                return candidates;
            }

            private static Shape BuildShape(
                Type type,
                JsonSerializerOptions options,
                Dictionary<Type, Shape> cache,
                JsonNumberHandling? numberHandling,
                int depth)
            {
                type = Nullable.GetUnderlyingType(type) ?? type;

                if (depth > MaxDepth)
                {
                    return Shape.Any;
                }

                if (cache.TryGetValue(type, out Shape? cached))
                {
                    return cached;
                }

                JsonTypeInfo typeInfo = options.GetTypeInfo(type);
                JsonNumberHandling? effectiveNumberHandling = numberHandling ?? typeInfo.NumberHandling ?? options.NumberHandling;

                cache[type] = Shape.Any;
                Shape result = typeInfo.Kind switch
                {
                    JsonTypeInfoKind.Object => BuildObjectShape(typeInfo, options, cache, effectiveNumberHandling, depth),
                    JsonTypeInfoKind.Enumerable => typeInfo.ElementType is Type elementType
                        ? Shape.CreateArray(BuildShape(elementType, options, cache, effectiveNumberHandling, depth + 1))
                        : Shape.Any,
                    JsonTypeInfoKind.Dictionary => typeInfo.ElementType is Type elementType
                        ? Shape.CreateDictionary(BuildShape(elementType, options, cache, effectiveNumberHandling, depth + 1))
                        : Shape.Any,
                    _ => CreateScalarShape(type, effectiveNumberHandling)
                };

                cache[type] = result;
                return result;
            }

            private static Shape BuildObjectShape(
                JsonTypeInfo typeInfo,
                JsonSerializerOptions options,
                Dictionary<Type, Shape> cache,
                JsonNumberHandling? numberHandling,
                int depth)
            {
                var properties = new List<PropertyShape>(typeInfo.Properties.Count);
                foreach (JsonPropertyInfo property in typeInfo.Properties)
                {
                    if (property.IsExtensionData)
                    {
                        continue;
                    }

                    JsonNumberHandling? propertyNumberHandling = property.NumberHandling ?? numberHandling;
                    properties.Add(new PropertyShape(
                        property.Name,
                        Encoding.UTF8.GetBytes(property.Name),
                        BuildShape(property.PropertyType, options, cache, propertyNumberHandling, depth + 1)));
                }

                return Shape.CreateObject(properties.ToArray(), options.PropertyNameCaseInsensitive);
            }

            private static Shape CreateScalarShape(Type type, JsonNumberHandling? numberHandling)
            {
                if (IsNumberType(type))
                {
                    return Shape.CreateNumber((numberHandling & JsonNumberHandling.AllowReadingFromString) != 0);
                }

                if (type == typeof(bool))
                {
                    return Shape.Boolean;
                }

                if (type == typeof(DateTime))
                {
                    return Shape.DateTimeString;
                }

                if (type == typeof(DateTimeOffset))
                {
                    return Shape.DateTimeOffsetString;
                }

                if (type == typeof(TimeSpan))
                {
                    return Shape.TimeSpanString;
                }

                if (type == typeof(Guid))
                {
                    return Shape.GuidString;
                }

                return type == typeof(string) || type == typeof(char) || type == typeof(Uri)
                    ? Shape.String
                    : Shape.Any;
            }

            private static Type? Classify(Candidate[] candidates, ref Utf8JsonReader reader)
            {
                Type? bestType = null;
                int bestScore = 0;
                bool isAmbiguous = false;

                for (int i = 0; i < candidates.Length; i++)
                {
                    Shape shape = candidates[i].Shape;
                    if (!CanMatchRootToken(reader.TokenType, shape))
                    {
                        continue;
                    }

                    Utf8JsonReader readerCopy = reader;
                    int score = ScoreValue(ref readerCopy, shape, depth: 0);
                    if (score > bestScore)
                    {
                        bestType = candidates[i].Type;
                        bestScore = score;
                        isAmbiguous = false;
                    }
                    else if (score == bestScore && score > 0)
                    {
                        isAmbiguous = true;
                    }
                }

                return bestScore > 0 && !isAmbiguous ? bestType : null;
            }

            private static bool CanMatchRootToken(JsonTokenType tokenType, Shape shape) =>
                shape.Kind is ShapeKind.Any ||
                tokenType switch
                {
                    JsonTokenType.StartObject => shape.Kind is ShapeKind.Object or ShapeKind.Dictionary,
                    JsonTokenType.StartArray => shape.Kind is ShapeKind.Array,
                    JsonTokenType.String => shape.Kind is ShapeKind.String || (shape.Kind is ShapeKind.Number && shape.AllowNumberFromString),
                    JsonTokenType.Number => shape.Kind is ShapeKind.Number,
                    JsonTokenType.True or JsonTokenType.False => shape.Kind is ShapeKind.Boolean,
                    _ => false
                };

            private static int ScoreValue(ref Utf8JsonReader reader, Shape shape, int depth)
            {
                if (depth > MaxDepth)
                {
                    return 0;
                }

                return shape.Kind switch
                {
                    ShapeKind.Any => 1,
                    ShapeKind.Boolean => reader.TokenType is JsonTokenType.True or JsonTokenType.False ? 4 : 0,
                    ShapeKind.Number => ScoreNumber(ref reader, shape),
                    ShapeKind.String => ScoreString(ref reader, shape.StringKind),
                    ShapeKind.Array => ScoreArray(ref reader, shape, depth),
                    ShapeKind.Object => ScoreObject(ref reader, shape, depth),
                    ShapeKind.Dictionary => ScoreDictionary(ref reader, shape, depth),
                    _ => 0
                };
            }

            private static int ScoreNumber(ref Utf8JsonReader reader, Shape shape)
            {
                if (reader.TokenType is JsonTokenType.Number)
                {
                    return 4;
                }

                return shape.AllowNumberFromString &&
                    reader.TokenType is JsonTokenType.String &&
                    ValueSpanIsContiguous(ref reader) &&
                    LooksLikeJsonNumber(reader.ValueSpan)
                    ? 3
                    : 0;
            }

            private static int ScoreString(ref Utf8JsonReader reader, StringKind stringKind)
            {
                if (reader.TokenType is not JsonTokenType.String)
                {
                    return 0;
                }

                return stringKind switch
                {
                    StringKind.Any => 4,
                    StringKind.DateTime => reader.TryGetDateTime(out _) ? 10 : 0,
                    StringKind.DateTimeOffset => reader.TryGetDateTimeOffset(out _) ? 10 : 0,
                    StringKind.TimeSpan => ValueSpanIsContiguous(ref reader) && LooksLikeTimeSpan(reader.ValueSpan) ? 10 : 0,
                    StringKind.Guid => reader.TryGetGuid(out _) ? 10 : 0,
                    _ => 0
                };
            }

            private static int ScoreArray(ref Utf8JsonReader reader, Shape shape, int depth)
            {
                if (reader.TokenType is not JsonTokenType.StartArray || shape.ElementShape is null)
                {
                    return 0;
                }

                int score = 4;
                int sampledElements = 0;
                while (sampledElements < 3 && reader.Read())
                {
                    if (reader.TokenType is JsonTokenType.EndArray)
                    {
                        break;
                    }

                    Utf8JsonReader elementReader = reader;
                    int elementScore = ScoreValue(ref elementReader, shape.ElementShape, depth + 1);
                    if (elementScore == 0)
                    {
                        return 0;
                    }

                    score += elementScore;
                    sampledElements++;
                    reader.TrySkip();
                }

                return sampledElements == 0 ? 4 : score;
            }

            private static int ScoreObject(ref Utf8JsonReader reader, Shape shape, int depth)
            {
                if (reader.TokenType is not JsonTokenType.StartObject)
                {
                    return 0;
                }

                int score = 4;
                while (reader.Read())
                {
                    if (reader.TokenType is JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType is not JsonTokenType.PropertyName)
                    {
                        return 0;
                    }

                    PropertyShape? property = FindProperty(shape, ref reader);
                    if (!reader.Read())
                    {
                        return score;
                    }

                    if (property is not null)
                    {
                        score += 8;

                        Utf8JsonReader valueReader = reader;
                        int valueScore = ScoreValue(ref valueReader, property.ValueShape, depth + 1);
                        if (valueScore > 0)
                        {
                            score += valueScore;
                        }
                    }

                    reader.TrySkip();
                }

                return score;
            }

            private static int ScoreDictionary(ref Utf8JsonReader reader, Shape shape, int depth)
            {
                if (reader.TokenType is not JsonTokenType.StartObject || shape.ElementShape is null)
                {
                    return 0;
                }

                int score = 4;
                int sampledProperties = 0;
                while (sampledProperties < 3 && reader.Read())
                {
                    if (reader.TokenType is JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType is not JsonTokenType.PropertyName || !reader.Read())
                    {
                        return 0;
                    }

                    Utf8JsonReader valueReader = reader;
                    int valueScore = ScoreValue(ref valueReader, shape.ElementShape, depth + 1);
                    if (valueScore == 0)
                    {
                        return 0;
                    }

                    score += 2 + valueScore;
                    sampledProperties++;
                    reader.TrySkip();
                }

                return sampledProperties == 0 ? 4 : score;
            }

            private static PropertyShape? FindProperty(Shape shape, ref Utf8JsonReader reader)
            {
                foreach (PropertyShape property in shape.Properties)
                {
                    if (reader.ValueTextEquals(property.NameUtf8))
                    {
                        return property;
                    }
                }

                if (!shape.PropertyNameCaseInsensitive || reader.HasValueSequence)
                {
                    return null;
                }

                ReadOnlySpan<byte> propertyName = reader.ValueSpan;
                foreach (PropertyShape property in shape.Properties)
                {
                    if (AsciiEqualsIgnoreCase(propertyName, property.NameUtf8))
                    {
                        return property;
                    }
                }

                return null;
            }

            private static bool AsciiEqualsIgnoreCase(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
            {
                if (left.Length != right.Length)
                {
                    return false;
                }

                for (int i = 0; i < left.Length; i++)
                {
                    byte l = left[i];
                    byte r = right[i];

                    if ((uint)(l - 'A') <= 'Z' - 'A')
                    {
                        l = (byte)(l + ('a' - 'A'));
                    }

                    if ((uint)(r - 'A') <= 'Z' - 'A')
                    {
                        r = (byte)(r + ('a' - 'A'));
                    }

                    if (l != r)
                    {
                        return false;
                    }
                }

                return true;
            }

            private static bool LooksLikeJsonNumber(ReadOnlySpan<byte> value)
            {
                if (value.IsEmpty)
                {
                    return false;
                }

                int index = value[0] == '-' ? 1 : 0;
                if ((uint)index >= (uint)value.Length || !IsDigit(value[index]))
                {
                    return false;
                }

                while ((uint)index < (uint)value.Length && IsDigit(value[index]))
                {
                    index++;
                }

                if ((uint)index < (uint)value.Length && value[index] == '.')
                {
                    index++;
                    if ((uint)index >= (uint)value.Length || !IsDigit(value[index]))
                    {
                        return false;
                    }

                    while ((uint)index < (uint)value.Length && IsDigit(value[index]))
                    {
                        index++;
                    }
                }

                if ((uint)index < (uint)value.Length && (value[index] == 'e' || value[index] == 'E'))
                {
                    index++;
                    if ((uint)index < (uint)value.Length && (value[index] == '+' || value[index] == '-'))
                    {
                        index++;
                    }

                    if ((uint)index >= (uint)value.Length || !IsDigit(value[index]))
                    {
                        return false;
                    }

                    while ((uint)index < (uint)value.Length && IsDigit(value[index]))
                    {
                        index++;
                    }
                }

                return index == value.Length;
            }

            private static bool LooksLikeTimeSpan(ReadOnlySpan<byte> value)
            {
                if (value.IsEmpty)
                {
                    return false;
                }

                int colonCount = 0;
                for (int i = 0; i < value.Length; i++)
                {
                    byte ch = value[i];
                    if (ch == ':')
                    {
                        colonCount++;
                    }
                    else if (ch != '-' && ch != '.' && !IsDigit(ch))
                    {
                        return false;
                    }
                }

                return colonCount >= 2;
            }

            private static bool ValueSpanIsContiguous(ref Utf8JsonReader reader) => !reader.HasValueSequence;

            private static bool IsDigit(byte value) => (uint)(value - '0') <= 9;

            private static bool IsNumberType(Type type)
            {
                type = Nullable.GetUnderlyingType(type) ?? type;
                return type == typeof(byte) ||
                    type == typeof(decimal) ||
                    type == typeof(double) ||
                    type == typeof(short) ||
                    type == typeof(int) ||
                    type == typeof(long) ||
                    type == typeof(sbyte) ||
                    type == typeof(float) ||
                    type == typeof(ushort) ||
                    type == typeof(uint) ||
                    type == typeof(ulong)
#if NET
                    || type == typeof(Half)
                    || type == typeof(Int128)
                    || type == typeof(UInt128)
#endif
                    ;
            }

            private sealed class Candidate(Type type, Shape shape)
            {
                public Type Type { get; } = type;
                public Shape Shape { get; } = shape;
            }

            private sealed class Shape
            {
                public static readonly Shape Any = new(ShapeKind.Any);
                public static readonly Shape Boolean = new(ShapeKind.Boolean);
                public static readonly Shape String = new(ShapeKind.String) { StringKind = StringKind.Any };
                public static readonly Shape DateTimeString = new(ShapeKind.String) { StringKind = StringKind.DateTime };
                public static readonly Shape DateTimeOffsetString = new(ShapeKind.String) { StringKind = StringKind.DateTimeOffset };
                public static readonly Shape TimeSpanString = new(ShapeKind.String) { StringKind = StringKind.TimeSpan };
                public static readonly Shape GuidString = new(ShapeKind.String) { StringKind = StringKind.Guid };

                private Shape(ShapeKind kind)
                {
                    Kind = kind;
                }

                public ShapeKind Kind { get; }
                public bool AllowNumberFromString { get; private init; }
                public bool PropertyNameCaseInsensitive { get; private init; }
                public StringKind StringKind { get; private init; }
                public PropertyShape[] Properties { get; private init; } = [];
                public Shape? ElementShape { get; private init; }

                public static Shape CreateNumber(bool allowNumberFromString) =>
                    new(ShapeKind.Number) { AllowNumberFromString = allowNumberFromString };

                public static Shape CreateObject(PropertyShape[] properties, bool propertyNameCaseInsensitive) =>
                    new(ShapeKind.Object) { Properties = properties, PropertyNameCaseInsensitive = propertyNameCaseInsensitive };

                public static Shape CreateArray(Shape elementShape) =>
                    new(ShapeKind.Array) { ElementShape = elementShape };

                public static Shape CreateDictionary(Shape elementShape) =>
                    new(ShapeKind.Dictionary) { ElementShape = elementShape };
            }

            private sealed class PropertyShape(string name, byte[] nameUtf8, Shape valueShape)
            {
                public string Name { get; } = name;
                public byte[] NameUtf8 { get; } = nameUtf8;
                public Shape ValueShape { get; } = valueShape;
            }

            private enum ShapeKind
            {
                Any,
                Boolean,
                Number,
                String,
                Object,
                Array,
                Dictionary
            }

            private enum StringKind
            {
                Any,
                DateTime,
                DateTimeOffset,
                TimeSpan,
                Guid
            }
        }

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
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

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union TemporalUnion(DateTime, TimeSpan, Guid);

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union ListUnion(List<int>, List<string>);

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union DictionaryUnion(Dictionary<string, int>, Dictionary<string, string>);

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union ArrayOrDictionaryUnion(List<int>, Dictionary<string, int>);

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
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

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union ObjectOrDictionaryUnion(Point, Dictionary<string, int>);

        public sealed class Point
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
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

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union NumberHandlingUnion(int, bool);

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union NumericUnion(int, long);

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union DateOrDateTimeOffsetUnion(DateTime, DateTimeOffset);

        [JsonUnion(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        public union IdenticalPetUnion(IdenticalDog, IdenticalCat);

        public sealed class IdenticalDog
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public sealed class IdenticalCat
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        [JsonPolymorphic(TypeClassifier = typeof(StructuralJsonTypeClassifierFactory))]
        [JsonDerivedType(typeof(Circle))]
        [JsonDerivedType(typeof(Rectangle))]
        public abstract class Shape
        {
            public string? Color { get; set; }
        }

        public sealed class Circle : Shape
        {
            public double Radius { get; set; }
        }

        public sealed class Rectangle : Shape
        {
            public double Width { get; set; }
            public double Height { get; set; }
        }
    }
}
