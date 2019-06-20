// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
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

            WhenClass when = JsonSerializer.Parse<WhenClass>(@"{""Day"":""Monday""}", options);
            Assert.Equal(DayOfWeek.Monday, when.Day);
            DayOfWeek day = JsonSerializer.Parse<DayOfWeek>(@"""Tuesday""", options);
            Assert.Equal(DayOfWeek.Tuesday, day);

            // We are case insensitive on read
            day = JsonSerializer.Parse<DayOfWeek>(@"""wednesday""", options);
            Assert.Equal(DayOfWeek.Wednesday, day);

            // Numbers work by default
            day = JsonSerializer.Parse<DayOfWeek>(@"4", options);
            Assert.Equal(DayOfWeek.Thursday, day);

            string json = JsonSerializer.ToString(DayOfWeek.Friday, options);
            Assert.Equal(@"""Friday""", json);

            // Try a unique naming policy
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(new ToLower()));

            json = JsonSerializer.ToString(DayOfWeek.Friday, options);
            Assert.Equal(@"""friday""", json);

            // Undefined values should come out as a number (not a string)
            json = JsonSerializer.ToString((DayOfWeek)(-1), options);
            Assert.Equal(@"-1", json);

            // Not permitting integers should throw
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            Assert.Throws<JsonException>(() => JsonSerializer.ToString((DayOfWeek)(-1), options));
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

            FileState state = JsonSerializer.Parse<FileState>(@"{""Attributes"":""ReadOnly""}", options);
            Assert.Equal(FileAttributes.ReadOnly, state.Attributes);
            state = JsonSerializer.Parse<FileState>(@"{""Attributes"":""Directory, ReparsePoint""}", options);
            Assert.Equal(FileAttributes.Directory | FileAttributes.ReparsePoint, state.Attributes);
            FileAttributes attributes = JsonSerializer.Parse<FileAttributes>(@"""Normal""", options);
            Assert.Equal(FileAttributes.Normal, attributes);
            attributes = JsonSerializer.Parse<FileAttributes>(@"""System, SparseFile""", options);
            Assert.Equal(FileAttributes.System | FileAttributes.SparseFile, attributes);

            // We are case insensitive on read
            attributes = JsonSerializer.Parse<FileAttributes>(@"""OFFLINE""", options);
            Assert.Equal(FileAttributes.Offline, attributes);
            attributes = JsonSerializer.Parse<FileAttributes>(@"""compressed, notcontentindexed""", options);
            Assert.Equal(FileAttributes.Compressed | FileAttributes.NotContentIndexed, attributes);

            // Numbers are cool by default
            attributes = JsonSerializer.Parse<FileAttributes>(@"131072", options);
            Assert.Equal(FileAttributes.NoScrubData, attributes);
            attributes = JsonSerializer.Parse<FileAttributes>(@"3", options);
            Assert.Equal(FileAttributes.Hidden | FileAttributes.ReadOnly, attributes);

            string json = JsonSerializer.ToString(FileAttributes.Hidden, options);
            Assert.Equal(@"""Hidden""", json);
            json = JsonSerializer.ToString(FileAttributes.Temporary | FileAttributes.Offline, options);
            Assert.Equal(@"""Temporary, Offline""", json);

            // Try a unique casing
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(new ToLower()));

            json = JsonSerializer.ToString(FileAttributes.NoScrubData, options);
            Assert.Equal(@"""noscrubdata""", json);
            json = JsonSerializer.ToString(FileAttributes.System | FileAttributes.Offline, options);
            Assert.Equal(@"""system, offline""", json);

            // Undefined values should come out as a number (not a string)
            json = JsonSerializer.ToString((FileAttributes)(-1), options);
            Assert.Equal(@"-1", json);

            // Not permitting integers should throw
            options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));
            Assert.Throws<JsonException>(() => JsonSerializer.ToString((FileAttributes)(-1), options));
        }

        public class FileState
        {
            public FileAttributes Attributes { get; set; }
        }
    }
}
