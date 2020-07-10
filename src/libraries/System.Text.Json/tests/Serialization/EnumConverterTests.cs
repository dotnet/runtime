// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class EnumConverterTests
    {
        [Fact]
        public void ConvertDayOfWeek()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

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
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(new ToLower()));

            json = JsonSerializer.Serialize(DayOfWeek.Friday, options);
            Assert.Equal(@"""friday""", json);

            // Undefined values should come out as a number (not a string)
            json = JsonSerializer.Serialize((DayOfWeek)(-1), options);
            Assert.Equal(@"-1", json);

            // Not permitting integers should throw
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            Assert.Throws<JsonException>(() => JsonSerializer.Serialize((DayOfWeek)(-1), options));
        }

        public class ToLower : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name.ToLowerInvariant();
        }

        public class WhenClass
        {
            public DayOfWeek Day { get; set; }
        }

        [Fact]
        public void ConvertFileAttributes()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

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
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(new ToLower()));

            json = JsonSerializer.Serialize(FileAttributes.NoScrubData, options);
            Assert.Equal(@"""noscrubdata""", json);
            json = JsonSerializer.Serialize(FileAttributes.System | FileAttributes.Offline, options);
            Assert.Equal(@"""system, offline""", json);

            // Undefined values should come out as a number (not a string)
            json = JsonSerializer.Serialize((FileAttributes)(-1), options);
            Assert.Equal(@"-1", json);

            // Not permitting integers should throw
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            Assert.Throws<JsonException>(() => JsonSerializer.Serialize((FileAttributes)(-1), options));

            // Flag values honor naming policy correctly
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(new SimpleSnakeCasePolicy()));

            json = JsonSerializer.Serialize(
                FileAttributes.Directory | FileAttributes.Compressed | FileAttributes.IntegrityStream,
                options);
            Assert.Equal(@"""directory, compressed, integrity_stream""", json);

            json = JsonSerializer.Serialize(FileAttributes.Compressed & FileAttributes.Device, options);
            Assert.Equal(@"0", json);

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
            [LowerCaseEnum]
            public DayOfWeek WeekEnd { get; set; }
        }

        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
        private class LowerCaseEnumAttribute : JsonConverterAttribute
        {
            public LowerCaseEnumAttribute() { }

            public override JsonConverter CreateConverter(Type typeToConvert)
                => new JsonStringEnumConverter(new ToLower());
        }

        [Fact]
        public void ConvertEnumUsingAttributes()
        {
            Week week = new Week { WorkStart = DayOfWeek.Monday, WorkEnd = DayOfWeek.Friday, WeekEnd = DayOfWeek.Saturday };
            string json = JsonSerializer.Serialize(week);
            Assert.Equal(@"{""WorkStart"":""Monday"",""WorkEnd"":5,""WeekEnd"":""saturday""}", json);

            week = JsonSerializer.Deserialize<Week>(json);
            Assert.Equal(DayOfWeek.Monday, week.WorkStart);
            Assert.Equal(DayOfWeek.Friday, week.WorkEnd);
            Assert.Equal(DayOfWeek.Saturday, week.WeekEnd);
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
            Second =2
        }

        [Fact]
        public void EnumWithConverterAttribute()
        {
            string json = JsonSerializer.Serialize(MyCustomEnum.Second);
            Assert.Equal(@"""Second""", json);

            MyCustomEnum obj = JsonSerializer.Deserialize<MyCustomEnum>("\"Second\"");
            Assert.Equal(MyCustomEnum.Second, obj);

            obj = JsonSerializer.Deserialize<MyCustomEnum>("2");
            Assert.Equal(MyCustomEnum.Second, obj);
        }

        [Fact]
        public static void EnumWithNoValues()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            Assert.Equal("-1", JsonSerializer.Serialize((EmptyEnum)(-1), options));
            Assert.Equal("1", JsonSerializer.Serialize((EmptyEnum)(1), options));
        }

        public enum EmptyEnum { };

        [Fact]
        public static void MoreThan64EnumValuesToSerialize()
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            };

            for (int i = 0; i < 128; i++)
            {
                MyEnum value = (MyEnum)i;
                string asStr = value.ToString();
                string expected = char.IsLetter(asStr[0]) ? $@"""{asStr}""" : asStr;
                Assert.Equal(expected, JsonSerializer.Serialize(value, options));
            }
        }

        [Fact, OuterLoop]
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
    }
}
