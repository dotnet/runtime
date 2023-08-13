// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class EnumConverterTests
    {
        [Theory]
        [InlineData(typeof(JsonStringEnumConverter), typeof(DayOfWeek))]
        [InlineData(typeof(JsonStringEnumConverter), typeof(MyCustomEnum))]
        [InlineData(typeof(JsonStringEnumConverter<DayOfWeek>), typeof(DayOfWeek))]
        [InlineData(typeof(JsonStringEnumConverter<MyCustomEnum>), typeof(MyCustomEnum))]
        public static void JsonStringEnumConverter_SupportedType_WorksAsExpected(Type converterType, Type supportedType)
        {
            var options = new JsonSerializerOptions();
            var factory = (JsonConverterFactory)Activator.CreateInstance(converterType);

            Assert.True(factory.CanConvert(supportedType));

            JsonConverter converter = factory.CreateConverter(supportedType, options);
            Assert.Equal(supportedType, converter.Type);
        }

        [Theory]
        [InlineData(typeof(JsonStringEnumConverter), typeof(int))]
        [InlineData(typeof(JsonStringEnumConverter), typeof(string))]
        [InlineData(typeof(JsonStringEnumConverter), typeof(JsonStringEnumConverter))]
        [InlineData(typeof(JsonStringEnumConverter<DayOfWeek>), typeof(int))]
        [InlineData(typeof(JsonStringEnumConverter<DayOfWeek>), typeof(string))]
        [InlineData(typeof(JsonStringEnumConverter<DayOfWeek>), typeof(JsonStringEnumConverter<MyCustomEnum>))]
        [InlineData(typeof(JsonStringEnumConverter<DayOfWeek>), typeof(MyCustomEnum))]
        [InlineData(typeof(JsonStringEnumConverter<MyCustomEnum>), typeof(DayOfWeek))]
        public static void JsonStringEnumConverter_InvalidType_ThrowsArgumentOutOfRangeException(Type converterType, Type unsupportedType)
        {
            var options = new JsonSerializerOptions();
            var factory = (JsonConverterFactory)Activator.CreateInstance(converterType);

            Assert.False(factory.CanConvert(unsupportedType));
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateConverter(unsupportedType, options));
            Assert.Contains(unsupportedType.FullName, ex.Message);
        }

        [Theory]
        [InlineData(typeof(JsonNumberEnumConverter<DayOfWeek>), typeof(DayOfWeek))]
        [InlineData(typeof(JsonNumberEnumConverter<MyCustomEnum>), typeof(MyCustomEnum))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(JsonNumberEnumConverter<>))]
        public static void JsonNumberEnumConverter_SupportedType_WorksAsExpected(Type converterType, Type supportedType)
        {
            var options = new JsonSerializerOptions();
            var factory = (JsonConverterFactory)Activator.CreateInstance(converterType);

            Assert.True(factory.CanConvert(supportedType));

            JsonConverter converter = factory.CreateConverter(supportedType, options);
            Assert.Equal(supportedType, converter.Type);
        }

        [Theory]
        [InlineData(typeof(JsonNumberEnumConverter<DayOfWeek>), typeof(int))]
        [InlineData(typeof(JsonNumberEnumConverter<DayOfWeek>), typeof(string))]
        [InlineData(typeof(JsonNumberEnumConverter<DayOfWeek>), typeof(JsonStringEnumConverter<MyCustomEnum>))]
        [InlineData(typeof(JsonNumberEnumConverter<DayOfWeek>), typeof(MyCustomEnum))]
        [InlineData(typeof(JsonNumberEnumConverter<MyCustomEnum>), typeof(DayOfWeek))]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, typeof(JsonNumberEnumConverter<>))]
        public static void JsonNumberEnumConverter_InvalidType_ThrowsArgumentOutOfRangeException(Type converterType, Type unsupportedType)
        {
            var options = new JsonSerializerOptions();
            var factory = (JsonConverterFactory)Activator.CreateInstance(converterType);

            Assert.False(factory.CanConvert(unsupportedType));
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => factory.CreateConverter(unsupportedType, options));
            Assert.Contains(unsupportedType.FullName, ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConvertDayOfWeek(bool useGenericVariant)
        {
            JsonSerializerOptions options = CreateStringEnumOptionsForType<DayOfWeek>(useGenericVariant);

            WhenClass when = JsonSerializer.Deserialize<WhenClass>(@"{""Day"":""Monday""}", options);
            Assert.Equal(DayOfWeek.Monday, when.Day);
            DayOfWeek day = JsonSerializer.Deserialize<DayOfWeek>(@"""Tuesday""", options);
            Assert.Equal(DayOfWeek.Tuesday, day);

            // We are case insensitive on read
            day = JsonSerializer.Deserialize<DayOfWeek>(@"""wednesday""", options);
            Assert.Equal(DayOfWeek.Wednesday, day);

            // Numbers work by default
            day = JsonSerializer.Deserialize<DayOfWeek>(@"4", options);
            Assert.Equal(DayOfWeek.Thursday, day);

            string json = JsonSerializer.Serialize(DayOfWeek.Friday, options);
            Assert.Equal(@"""Friday""", json);

            // Try a unique naming policy
            options = CreateStringEnumOptionsForType<DayOfWeek>(useGenericVariant, new ToLowerNamingPolicy());

            json = JsonSerializer.Serialize(DayOfWeek.Friday, options);
            Assert.Equal(@"""friday""", json);

            // Undefined values should come out as a number (not a string)
            json = JsonSerializer.Serialize((DayOfWeek)(-1), options);
            Assert.Equal(@"-1", json);

            // Not permitting integers should throw
            options = CreateStringEnumOptionsForType<DayOfWeek>(useGenericVariant, allowIntegerValues: false);
            Assert.Throws<JsonException>(() => JsonSerializer.Serialize((DayOfWeek)(-1), options));
        }

        public class ToLowerNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name.ToLowerInvariant();
        }

        public class WhenClass
        {
            public DayOfWeek Day { get; set; }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ConvertFileAttributes(bool useGenericVariant)
        {
            JsonSerializerOptions options = CreateStringEnumOptionsForType<FileAttributes>(useGenericVariant);

            FileState state = JsonSerializer.Deserialize<FileState>(@"{""Attributes"":""ReadOnly""}", options);
            Assert.Equal(FileAttributes.ReadOnly, state.Attributes);
            state = JsonSerializer.Deserialize<FileState>(@"{""Attributes"":""Directory, ReparsePoint""}", options);
            Assert.Equal(FileAttributes.Directory | FileAttributes.ReparsePoint, state.Attributes);
            FileAttributes attributes = JsonSerializer.Deserialize<FileAttributes>(@"""Normal""", options);
            Assert.Equal(FileAttributes.Normal, attributes);
            attributes = JsonSerializer.Deserialize<FileAttributes>(@"""System, SparseFile""", options);
            Assert.Equal(FileAttributes.System | FileAttributes.SparseFile, attributes);

            // We are case insensitive on read
            attributes = JsonSerializer.Deserialize<FileAttributes>(@"""OFFLINE""", options);
            Assert.Equal(FileAttributes.Offline, attributes);
            attributes = JsonSerializer.Deserialize<FileAttributes>(@"""compressed, notcontentindexed""", options);
            Assert.Equal(FileAttributes.Compressed | FileAttributes.NotContentIndexed, attributes);

            // Numbers are cool by default
            attributes = JsonSerializer.Deserialize<FileAttributes>(@"131072", options);
            Assert.Equal(FileAttributes.NoScrubData, attributes);
            attributes = JsonSerializer.Deserialize<FileAttributes>(@"3", options);
            Assert.Equal(FileAttributes.Hidden | FileAttributes.ReadOnly, attributes);

            string json = JsonSerializer.Serialize(FileAttributes.Hidden, options);
            Assert.Equal(@"""Hidden""", json);
            json = JsonSerializer.Serialize(FileAttributes.Temporary | FileAttributes.Offline, options);
            Assert.Equal(@"""Temporary, Offline""", json);

            // Try a unique casing
            options = CreateStringEnumOptionsForType<FileAttributes>(useGenericVariant, new ToLowerNamingPolicy());

            json = JsonSerializer.Serialize(FileAttributes.NoScrubData, options);
            Assert.Equal(@"""noscrubdata""", json);
            json = JsonSerializer.Serialize(FileAttributes.System | FileAttributes.Offline, options);
            Assert.Equal(@"""system, offline""", json);

            // Undefined values should come out as a number (not a string)
            json = JsonSerializer.Serialize((FileAttributes)(-1), options);
            Assert.Equal(@"-1", json);

            // Not permitting integers should throw
            options = CreateStringEnumOptionsForType<FileAttributes>(useGenericVariant, allowIntegerValues: false);
            Assert.Throws<JsonException>(() => JsonSerializer.Serialize((FileAttributes)(-1), options));

            // Flag values honor naming policy correctly
            options = CreateStringEnumOptionsForType<FileAttributes>(useGenericVariant, new SimpleSnakeCasePolicy());

            json = JsonSerializer.Serialize(
                FileAttributes.Directory | FileAttributes.Compressed | FileAttributes.IntegrityStream,
                options);
            Assert.Equal(@"""directory, compressed, integrity_stream""", json);

            json = JsonSerializer.Serialize((FileAttributes)(-1), options);
            Assert.Equal(@"-1", json);

            json = JsonSerializer.Serialize(FileAttributes.Directory & FileAttributes.Compressed | FileAttributes.IntegrityStream, options);
            Assert.Equal(@"""integrity_stream""", json);
        }

        public class FileState
        {
            public FileAttributes Attributes { get; set; }
        }

        public class Week
        {
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public DayOfWeek WorkStart { get; set; }
            public DayOfWeek WorkEnd { get; set; }
            [JsonConverter(typeof(LowerCaseEnumConverter))]
            public DayOfWeek WeekEnd { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter<DayOfWeek>))]
            public DayOfWeek WorkStart2 { get; set; }
            [JsonConverter(typeof(LowerCaseEnumConverter<DayOfWeek>))]
            public DayOfWeek WeekEnd2 { get; set; }
        }

        private class LowerCaseEnumConverter : JsonStringEnumConverter
        {
            public LowerCaseEnumConverter() : base(new ToLowerNamingPolicy())
            {
            }
        }

        private class LowerCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
            where TEnum : struct, Enum
        {
            public LowerCaseEnumConverter() : base(new ToLowerNamingPolicy())
            {
            }
        }

        [Fact]
        public void ConvertEnumUsingAttributes()
        {
            Week week = new Week {
                WorkStart = DayOfWeek.Monday,
                WorkEnd = DayOfWeek.Friday,
                WeekEnd = DayOfWeek.Saturday,
                WorkStart2 = DayOfWeek.Tuesday,
                WeekEnd2 = DayOfWeek.Thursday,
            };

            string json = JsonSerializer.Serialize(week);
            Assert.Equal("""{"WorkStart":"Monday","WorkEnd":5,"WeekEnd":"saturday","WorkStart2":"Tuesday","WeekEnd2":"thursday"}""", json);

            week = JsonSerializer.Deserialize<Week>(json);
            Assert.Equal(DayOfWeek.Monday, week.WorkStart);
            Assert.Equal(DayOfWeek.Friday, week.WorkEnd);
            Assert.Equal(DayOfWeek.Saturday, week.WeekEnd);
            Assert.Equal(DayOfWeek.Tuesday, week.WorkStart2);
            Assert.Equal(DayOfWeek.Thursday, week.WeekEnd2);
        }

        [Fact]
        public void EnumConverterComposition()
        {
            JsonSerializerOptions options = new JsonSerializerOptions { Converters = { new NoFlagsStringEnumConverter() } };
            string json = JsonSerializer.Serialize(DayOfWeek.Monday, options);
            Assert.Equal(@"""Monday""", json);
            json = JsonSerializer.Serialize(FileAccess.Read);
            Assert.Equal(@"1", json);
        }

        public class NoFlagsStringEnumConverter : JsonConverterFactory
        {
            private static JsonStringEnumConverter s_stringEnumConverter = new JsonStringEnumConverter();

            public override bool CanConvert(Type typeToConvert)
                => typeToConvert.IsEnum && !typeToConvert.IsDefined(typeof(FlagsAttribute), inherit: false);

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
                => s_stringEnumConverter.CreateConverter(typeToConvert, options);
        }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        private enum MyCustomEnum
        {
            First = 1,
            Second = 2
        }

        [JsonConverter(typeof(JsonStringEnumConverter<MyCustomEnum2>))]
        private enum MyCustomEnum2
        {
            First = 1,
            Second = 2
        }

        [Theory]
        [InlineData(typeof(MyCustomEnum), MyCustomEnum.Second, "\"Second\"", "2")]
        [InlineData(typeof(MyCustomEnum2), MyCustomEnum2.Second, "\"Second\"", "2")]
        public void EnumWithConverterAttribute(Type enumType, object value, string expectedJson, string alternativeJson)
        {
            string json = JsonSerializer.Serialize(value, enumType);
            Assert.Equal(expectedJson, json);

            object? result = JsonSerializer.Deserialize(json, enumType);
            Assert.Equal(value, result);

            result = JsonSerializer.Deserialize(alternativeJson, enumType);
            Assert.Equal(value, result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void EnumWithNoValues(bool useGenericVariant)
        {
            JsonSerializerOptions options = CreateStringEnumOptionsForType<EmptyEnum>(useGenericVariant);

            Assert.Equal("-1", JsonSerializer.Serialize((EmptyEnum)(-1), options));
            Assert.Equal("1", JsonSerializer.Serialize((EmptyEnum)(1), options));
        }

        public enum EmptyEnum { };

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MoreThan64EnumValuesToSerialize(bool useGenericVariant)
        {
            JsonSerializerOptions options = CreateStringEnumOptionsForType<MyEnum>(useGenericVariant);

            for (int i = 0; i < 128; i++)
            {
                MyEnum value = (MyEnum)i;
                string asStr = value.ToString();
                string expected = char.IsLetter(asStr[0]) ? $@"""{asStr}""" : asStr;
                Assert.Equal(expected, JsonSerializer.Serialize(value, options));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void MoreThan64EnumValuesToSerializeWithNamingPolicy(bool useGenericVariant)
        {
            JsonSerializerOptions options = CreateStringEnumOptionsForType<MyEnum>(useGenericVariant, new ToLowerNamingPolicy());

            for (int i = 0; i < 128; i++)
            {
                MyEnum value = (MyEnum)i;
                string asStr = value.ToString().ToLowerInvariant();
                string expected = char.IsLetter(asStr[0]) ? $@"""{asStr}""" : asStr;
                Assert.Equal(expected, JsonSerializer.Serialize(value, options));
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [OuterLoop]
        public static void VeryLargeAmountOfEnumsToSerialize()
        {
            // Ensure we don't throw OutOfMemoryException.

            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            const int MaxValue = 2097152; // value for MyEnum.V

            // Every value between 0 and MaxValue maps to a valid enum
            // identifier, and is a candidate to go into the name cache.

            // Write the first 45 values.
            for (int i = 1; i < 46; i++)
            {
                JsonSerializer.Serialize((MyEnum)i, options);
            }

            // At this point, there are 60 values in the name cache;
            // 22 cached at warm-up, the rest in the above loop.

            // Ensure the approximate size limit for the name cache (a concurrent dictionary) is honored.
            // Use multiple threads to perhaps go over the soft limit of 64, but not by more than a couple.
            Parallel.For(0, 8, i => JsonSerializer.Serialize((MyEnum)(46 + i), options));

            // Write the remaining enum values. The cache is capped to avoid
            // OutOfMemoryException due to having too many cached items.
            for (int i = 54; i <= MaxValue; i++)
            {
                JsonSerializer.Serialize((MyEnum)i, options);
            }
        }

        [Flags]
        public enum MyEnum
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
            H = 1 << 7,
            I = 1 << 8,
            J = 1 << 9,
            K = 1 << 10,
            L = 1 << 11,
            M = 1 << 12,
            N = 1 << 13,
            O = 1 << 14,
            P = 1 << 15,
            Q = 1 << 16,
            R = 1 << 17,
            S = 1 << 18,
            T = 1 << 19,
            U = 1 << 20,
            V = 1 << 21,
        }

        [Fact, OuterLoop]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/42677", platforms: TestPlatforms.Windows, runtimes: TestRuntimes.Mono)]
        public static void VeryLargeAmountOfEnumDictionaryKeysToSerialize()
        {
            // Ensure we don't throw OutOfMemoryException.

            const int MaxValue = (int)MyEnum.V;

            // Every value between 0 and MaxValue maps to a valid enum
            // identifier, and is a candidate to go into the name cache.

            // Write the first 45 values.
            Dictionary<MyEnum, int> dictionary;
            for (int i = 1; i < 46; i++)
            {
                dictionary = new Dictionary<MyEnum, int> { { (MyEnum)i, i } };
                JsonSerializer.Serialize(dictionary);
            }

            // At this point, there are 60 values in the name cache;
            // 22 cached at warm-up, the rest in the above loop.

            // Ensure the approximate size limit for the name cache (a concurrent dictionary) is honored.
            // Use multiple threads to perhaps go over the soft limit of 64, but not by more than a couple.
            Parallel.For(
                0,
                8,
                i =>
                {
                    dictionary = new Dictionary<MyEnum, int> { { (MyEnum)(46 + i), i } };
                    JsonSerializer.Serialize(dictionary);
                }
            );

            // Write the remaining enum values. The cache is capped to avoid
            // OutOfMemoryException due to having too many cached items.
            for (int i = 54; i <= MaxValue; i++)
            {
                dictionary = new Dictionary<MyEnum, int> { { (MyEnum)i, i } };
                JsonSerializer.Serialize(dictionary);
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void NegativeEnumValue_CultureInvariance()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/68600
            RemoteExecutor.Invoke(static () =>
            {
                SampleEnumInt32 value = (SampleEnumInt32)(-2);
                string expectedJson = "-2";

                var options = new JsonSerializerOptions
                {
                    Converters = { new JsonStringEnumConverter(allowIntegerValues: true) },
                };

                // Sets the minus sign to -
                CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

                string actualJson = JsonSerializer.Serialize(value, options);
                Assert.Equal(expectedJson, actualJson);
                SampleEnumInt32 result = JsonSerializer.Deserialize<SampleEnumInt32>(actualJson, options);
                Assert.Equal(value, result);

                // Sets the minus sign to U+2212
                CultureInfo.CurrentCulture = new CultureInfo("sv-SE");

                actualJson = JsonSerializer.Serialize(value, options);
                Assert.Equal(expectedJson, actualJson);
                result = JsonSerializer.Deserialize<SampleEnumInt32>(actualJson, options);
                Assert.Equal(value, result);
            }).Dispose();
        }

        public abstract class NumericEnumKeyDictionaryBase<T>
        {
            public abstract Dictionary<T, int> BuildDictionary(int i);

            [Fact]
            public void SerilizeDictionaryWhenCacheIsFull()
            {
                Dictionary<T, int> dictionary;
                for (int i = 1; i <= 64; i++)
                {
                    dictionary = BuildDictionary(i);
                    JsonSerializer.Serialize(dictionary);
                }

                dictionary = BuildDictionary(0);
                string json = JsonSerializer.Serialize(dictionary);
                Assert.Equal($"{{\"0\":0}}", json);
            }
        }

        public class Int32EnumDictionary : NumericEnumKeyDictionaryBase<SampleEnumInt32>
        {
            public override Dictionary<SampleEnumInt32, int> BuildDictionary(int i) =>
                new Dictionary<SampleEnumInt32, int> { { (SampleEnumInt32)i, i } };
        }

        public class UInt32EnumDictionary : NumericEnumKeyDictionaryBase<SampleEnumUInt32>
        {
            public override Dictionary<SampleEnumUInt32, int> BuildDictionary(int i) =>
                new Dictionary<SampleEnumUInt32, int> { { (SampleEnumUInt32)i, i } };
        }

        public class UInt64EnumDictionary : NumericEnumKeyDictionaryBase<SampleEnumUInt64>
        {
            public override Dictionary<SampleEnumUInt64, int> BuildDictionary(int i) =>
                new Dictionary<SampleEnumUInt64, int> { { (SampleEnumUInt64)i, i } };
        }

        public class Int64EnumDictionary : NumericEnumKeyDictionaryBase<SampleEnumInt64>
        {
            public override Dictionary<SampleEnumInt64, int> BuildDictionary(int i) =>
                new Dictionary<SampleEnumInt64, int> { { (SampleEnumInt64)i, i } };
        }

        public class Int16EnumDictionary : NumericEnumKeyDictionaryBase<SampleEnumInt16>
        {
            public override Dictionary<SampleEnumInt16, int> BuildDictionary(int i) =>
                new Dictionary<SampleEnumInt16, int> { { (SampleEnumInt16)i, i } };
        }

        public class UInt16EnumDictionary : NumericEnumKeyDictionaryBase<SampleEnumUInt16>
        {
            public override Dictionary<SampleEnumUInt16, int> BuildDictionary(int i) =>
                new Dictionary<SampleEnumUInt16, int> { { (SampleEnumUInt16)i, i } };
        }

        public class ByteEnumDictionary : NumericEnumKeyDictionaryBase<SampleEnumByte>
        {
            public override Dictionary<SampleEnumByte, int> BuildDictionary(int i) =>
                new Dictionary<SampleEnumByte, int> { { (SampleEnumByte)i, i } };
        }

        public class SByteEnumDictionary : NumericEnumKeyDictionaryBase<SampleEnumSByte>
        {
            public override Dictionary<SampleEnumSByte, int> BuildDictionary(int i) =>
                new Dictionary<SampleEnumSByte, int> { { (SampleEnumSByte)i, i } };
        }


        [Flags]
        public enum SampleEnumInt32
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
        }

        [Flags]
        public enum SampleEnumUInt32 : uint
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
        }

        [Flags]
        public enum SampleEnumUInt64 : ulong
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
        }

        [Flags]
        public enum SampleEnumInt64 : long
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
        }

        [Flags]
        public enum SampleEnumInt16 : short
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
        }

        [Flags]
        public enum SampleEnumUInt16 : ushort
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
        }

        [Flags]
        public enum SampleEnumByte : byte
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
        }

        [Flags]
        public enum SampleEnumSByte : sbyte
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
            D = 1 << 3,
            E = 1 << 4,
            F = 1 << 5,
            G = 1 << 6,
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void Honor_EnumNamingPolicy_On_Deserialization(bool useGenericVariant)
        {
            JsonSerializerOptions options = CreateStringEnumOptionsForType<BindingFlags>(useGenericVariant, new SimpleSnakeCasePolicy());

            BindingFlags bindingFlags = JsonSerializer.Deserialize<BindingFlags>(@"""non_public""", options);
            Assert.Equal(BindingFlags.NonPublic, bindingFlags);

            // Flags supported without naming policy.
            bindingFlags = JsonSerializer.Deserialize<BindingFlags>(@"""NonPublic, Public""", options);
            Assert.Equal(BindingFlags.NonPublic | BindingFlags.Public, bindingFlags);

            // Flags supported with naming policy.
            bindingFlags = JsonSerializer.Deserialize<BindingFlags>(@"""static, public""", options);
            Assert.Equal(BindingFlags.Static | BindingFlags.Public, bindingFlags);

            // Null not supported.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BindingFlags>("null", options));

            // Null supported for nullable enum.
            Assert.Null(JsonSerializer.Deserialize<BindingFlags?>("null", options));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static void EnumDictionaryKeyDeserialization(bool useGenericVariant)
        {
            JsonNamingPolicy snakeCasePolicy = new SimpleSnakeCasePolicy();
            JsonSerializerOptions options = CreateStringEnumOptionsForType<BindingFlags>(useGenericVariant);
            options.DictionaryKeyPolicy = snakeCasePolicy;

            // Baseline.
            var dict = JsonSerializer.Deserialize<Dictionary<BindingFlags, int>>(@"{""NonPublic, Public"": 1}", options);
            Assert.Equal(1, dict[BindingFlags.NonPublic | BindingFlags.Public]);

            // DictionaryKeyPolicy not honored for dict key deserialization.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<BindingFlags, int>>(@"{""NonPublic0, Public0"": 1}", options));

            // EnumConverter naming policy not honored.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<BindingFlags, int>>(@"{""non_public, static"": 0, ""NonPublic, Public"": 1}", options));
        }

        [Fact]
        public static void EnumDictionaryKeySerialization()
        {
            JsonSerializerOptions options = new()
            {
                DictionaryKeyPolicy = new SimpleSnakeCasePolicy()
            };

            Dictionary<BindingFlags, int> dict = new()
            {
                [BindingFlags.NonPublic | BindingFlags.Public] = 1,
                [BindingFlags.Static] = 2,
            };

            string expected = @"{
    ""public, non_public"": 1,
    ""static"": 2
}";

            JsonTestHelper.AssertJsonEqual(expected, JsonSerializer.Serialize(dict, options));
        }

        private class ZeroAppenderPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name + "0";
        }

        private static JsonSerializerOptions CreateStringEnumOptionsForType<TEnum>(bool useGenericVariant, JsonNamingPolicy? namingPolicy = null, bool allowIntegerValues = true) where TEnum : struct, Enum
        {
            return new JsonSerializerOptions
            {
                Converters =
                {
                    useGenericVariant
                    ? new JsonStringEnumConverter<TEnum>(namingPolicy, allowIntegerValues)
                    : new JsonStringEnumConverter(namingPolicy, allowIntegerValues)
                }
            };
        }
    }
}
