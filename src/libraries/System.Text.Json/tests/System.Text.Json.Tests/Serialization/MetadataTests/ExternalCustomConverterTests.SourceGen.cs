// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ExternalCustomConverterSourceGenTests
    {
        [Fact]
        public static void ExternalConverterOnBaseContext_RoundTripsUnderlyingNullableType()
        {
            var value = new ExternalModel
            {
                Id = new ExternalId(1),
                NullableId = new ExternalId(2),
            };

            string json = JsonSerializer.Serialize(value, InheritedContext.Default.ExternalModel);
            JsonTestHelper.AssertJsonEqual("""{"Id":1,"NullableId":2}""", json);

            ExternalModel deserialized = JsonSerializer.Deserialize(json, InheritedContext.Default.ExternalModel);
            Assert.Equal(1, deserialized.Id.Value);
            Assert.True(deserialized.NullableId.HasValue);
            Assert.Equal(2, deserialized.NullableId.Value.Value);
        }

        [Fact]
        public static void ExternalConverterTakesPrecedenceOverTypeLevelConverter()
        {
            var value = new PrecedenceModel { Id = new PrecedenceId(1) };

            string json = JsonSerializer.Serialize(value, PrecedenceModelContext.Default.PrecedenceModel);
            JsonTestHelper.AssertJsonEqual("""{"Id":1001}""", json);

            PrecedenceModel deserialized = JsonSerializer.Deserialize(json, PrecedenceModelContext.Default.PrecedenceModel);
            Assert.Equal(1, deserialized.Id.Value);
        }

        [Fact]
        public static void ExactNullableExternalConverterRegistrationTakesPrecedence()
        {
            var value = new NullableOverrideModel
            {
                Id = new NullableOverrideId(3),
                NullableId = new NullableOverrideId(4),
            };

            string json = JsonSerializer.Serialize(value, NullableOverrideContext.Default.NullableOverrideModel);
            JsonTestHelper.AssertJsonEqual("""{"Id":3,"NullableId":"n:4"}""", json);

            NullableOverrideModel deserialized = JsonSerializer.Deserialize(json, NullableOverrideContext.Default.NullableOverrideModel);
            Assert.Equal(3, deserialized.Id.Value);
            Assert.True(deserialized.NullableId.HasValue);
            Assert.Equal(4, deserialized.NullableId.Value.Value);
        }

        [Fact]
        public static void NullableConverterIsUsedForNullValue()
        {
            var value = new NullableOverrideModel
            {
                Id = new NullableOverrideId(3),
                NullableId = null,
            };

            string json = JsonSerializer.Serialize(value, NullableOverrideContext.Default.NullableOverrideModel);
            JsonTestHelper.AssertJsonEqual("""{"Id":3,"NullableId":"n:null"}""", json);

            NullableOverrideModel deserialized = JsonSerializer.Deserialize(json, NullableOverrideContext.Default.NullableOverrideModel);
            Assert.Equal(3, deserialized.Id.Value);
            Assert.Null(deserialized.NullableId);
        }

        [Fact]
        public static void MemberLevelConvertersTakePrecedenceOverExternalConverter()
        {
            var value = new MemberOverrideModel
            {
                PropertyId = new MemberOverrideId(1),
                FieldId = new MemberOverrideId(2),
            };

            string json = JsonSerializer.Serialize(value, MemberOverrideContext.Default.MemberOverrideModel);
            JsonTestHelper.AssertJsonEqual("""{"PropertyId":101,"FieldId":202}""", json);

            MemberOverrideModel deserialized = JsonSerializer.Deserialize(json, MemberOverrideContext.Default.MemberOverrideModel);
            Assert.Equal(1, deserialized.PropertyId.Value);
            Assert.Equal(2, deserialized.FieldId.Value);
        }

        public sealed class ExternalModel
        {
            public ExternalId Id { get; set; }
            public ExternalId? NullableId { get; set; }
        }

        public readonly struct ExternalId
        {
            public ExternalId(int value) => Value = value;
            public int Value { get; }
        }

        public sealed class ExternalIdConverter : JsonConverter<ExternalId>
        {
            public override ExternalId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new(reader.GetInt32());

            public override void Write(Utf8JsonWriter writer, ExternalId value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.Value);
        }

        [JsonExternalConverter(typeof(ExternalIdConverter))]
        private abstract class InheritedContextBase : JsonSerializerContext
        {
            protected InheritedContextBase(JsonSerializerOptions? options) : base(options) { }
        }

        [JsonSerializable(typeof(ExternalModel))]
        private partial class InheritedContext : InheritedContextBase { }

        public sealed class PrecedenceModel
        {
            public PrecedenceId Id { get; set; }
        }

        [JsonConverter(typeof(PrecedenceIdTypeLevelConverter))]
        public readonly struct PrecedenceId
        {
            public PrecedenceId(int value) => Value = value;
            public int Value { get; }
        }

        public sealed class PrecedenceIdTypeLevelConverter : JsonConverter<PrecedenceId>
        {
            public override PrecedenceId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new(reader.GetInt32() - 100);

            public override void Write(Utf8JsonWriter writer, PrecedenceId value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.Value + 100);
        }

        public sealed class PrecedenceIdExternalConverter : JsonConverter<PrecedenceId>
        {
            public override PrecedenceId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new(reader.GetInt32() - 1000);

            public override void Write(Utf8JsonWriter writer, PrecedenceId value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.Value + 1000);
        }

        [JsonSerializable(typeof(PrecedenceModel))]
        [JsonExternalConverter(typeof(PrecedenceIdExternalConverter))]
        private partial class PrecedenceModelContext : JsonSerializerContext { }

        public sealed class NullableOverrideModel
        {
            public NullableOverrideId Id { get; set; }
            public NullableOverrideId? NullableId { get; set; }
        }

        public readonly struct NullableOverrideId
        {
            public NullableOverrideId(int value) => Value = value;
            public int Value { get; }
        }

        public sealed class NullableOverrideIdConverter : JsonConverter<NullableOverrideId>
        {
            public override NullableOverrideId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new(reader.GetInt32());

            public override void Write(Utf8JsonWriter writer, NullableOverrideId value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.Value);
        }

        public sealed class NullableOverrideNullableIdConverter : JsonConverter<NullableOverrideId?>
        {
            public override bool HandleNull => true;

            public override NullableOverrideId? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string value = reader.GetString();
                Assert.NotNull(value);
                Assert.StartsWith("n:", value, StringComparison.Ordinal);

                if (value == "n:null")
                {
                    return null;
                }
                return new NullableOverrideId(int.Parse(value.Substring(2)));
            }

            public override void Write(Utf8JsonWriter writer, NullableOverrideId? value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.HasValue ? $"n:{value.Value.Value}" : "n:null");
            }
        }

        [JsonSerializable(typeof(NullableOverrideModel))]
        [JsonExternalConverter(typeof(NullableOverrideIdConverter))]
        [JsonExternalConverter(typeof(NullableOverrideNullableIdConverter))]
        private partial class NullableOverrideContext : JsonSerializerContext { }

        public sealed class MemberOverrideModel
        {
            [JsonConverter(typeof(MemberOverridePropertyConverter))]
            public MemberOverrideId PropertyId { get; set; }

            [JsonInclude]
            [JsonConverter(typeof(MemberOverrideFieldConverter))]
            public MemberOverrideId FieldId;
        }

        public readonly struct MemberOverrideId
        {
            public MemberOverrideId(int value) => Value = value;
            public int Value { get; }
        }

        public sealed class MemberOverrideExternalConverter : JsonConverter<MemberOverrideId>
        {
            public override MemberOverrideId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new(reader.GetInt32() - 1000);

            public override void Write(Utf8JsonWriter writer, MemberOverrideId value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.Value + 1000);
        }

        public sealed class MemberOverridePropertyConverter : JsonConverter<MemberOverrideId>
        {
            public override MemberOverrideId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new(reader.GetInt32() - 100);

            public override void Write(Utf8JsonWriter writer, MemberOverrideId value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.Value + 100);
        }

        public sealed class MemberOverrideFieldConverter : JsonConverter<MemberOverrideId>
        {
            public override MemberOverrideId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => new(reader.GetInt32() - 200);

            public override void Write(Utf8JsonWriter writer, MemberOverrideId value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.Value + 200);
        }

        [JsonSerializable(typeof(MemberOverrideModel))]
        [JsonExternalConverter(typeof(MemberOverrideExternalConverter))]
        private partial class MemberOverrideContext : JsonSerializerContext { }
    }
}
