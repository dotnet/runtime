// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System.Globalization;
using Xunit;

namespace System.Text.Json.Tests
{
    public class DateTimeConverterTests
    {
        internal static string GetUtcOffsetText(DateTime d)
        {
            TimeSpan utcOffset = TimeZoneInfo.Local.GetUtcOffset(d);

            return utcOffset.Hours.ToString("+00;-00", CultureInfo.InvariantCulture) + ":" + utcOffset.Minutes.ToString("00;00", CultureInfo.InvariantCulture);
        }

        [Fact]
        public void SerializeDateTime()
        {
            DateTime d = new DateTime(2000, 12, 15, 22, 11, 3, 55, DateTimeKind.Utc);
            string result;

            result = JsonSerializer.Serialize(d);
            Assert.Equal(@"""2000-12-15T22:11:03.055Z""", result);

            Assert.Equal(d, JsonSerializer.Deserialize<DateTime>(result));

            d = new DateTime(2000, 12, 15, 22, 11, 3, 55, DateTimeKind.Local);
            result = JsonSerializer.Serialize(d);
            Assert.Equal(@"""2000-12-15T22:11:03.055" + GetUtcOffsetText(d) + @"""", result);
        }

        [Fact]
        public void SerializeDateTimeOffset()
        {
            DateTimeOffset d = new DateTimeOffset(2000, 12, 15, 22, 11, 3, 55, TimeSpan.Zero);
            string result;

            result = JsonSerializer.Serialize(d);
            Assert.Equal(@"""2000-12-15T22:11:03.055+00:00""", result);

            Assert.Equal(d, JsonSerializer.Deserialize<DateTimeOffset>(result));
        }

        [Fact]
        public void DeserializeDateTimeOffset()
        {
            // Intentionally use an offset that is unlikely in the real world,
            // so the test will be accurate regardless of the local time zone setting.
            TimeSpan offset = new TimeSpan(2, 15, 0);
            DateTimeOffset dto = new DateTimeOffset(2014, 1, 1, 0, 0, 0, 0, offset);

            DateTimeOffset test = JsonSerializer.Deserialize<DateTimeOffset>("\"2014-01-01T00:00:00+02:15\"");

            Assert.Equal(dto, test);
            Assert.Equal(dto.ToString("o"), test.ToString("o"));
        }

        [Fact]
        public void NullableSerializeUTC()
        {
            NullableDateTimeTestClass c = new NullableDateTimeTestClass();
            c.DateTimeField = new DateTime(2008, 12, 12, 12, 12, 12, 0, DateTimeKind.Utc).ToLocalTime();
            c.DateTimeOffsetField = new DateTime(2008, 12, 12, 12, 12, 12, 0, DateTimeKind.Utc).ToLocalTime();
            c.PreField = "Pre";
            c.PostField = "Post";
            string json = JsonSerializer.Serialize(c);

            NullableDateTimeTestClass newOne = JsonSerializer.Deserialize<NullableDateTimeTestClass>(json);
            Assert.Equal(newOne.DateTimeField, c.DateTimeField);
            Assert.Equal(newOne.DateTimeOffsetField, c.DateTimeOffsetField);
            Assert.Equal(newOne.PostField, c.PostField);
            Assert.Equal(newOne.PreField, c.PreField);

            c.DateTimeField = null;
            c.DateTimeOffsetField = null;
            c.PreField = "Pre";
            c.PostField = "Post";
            json = JsonSerializer.Serialize(c);
            Assert.Equal(@"{""PreField"":""Pre"",""DateTimeField"":null,""DateTimeOffsetField"":null,""PostField"":""Post""}", json);
        }

        [Fact]
        public void SerializeUTC()
        {
            DateTimeTestClass c = new DateTimeTestClass();
            c.DateTimeField = new DateTime(2008, 12, 12, 12, 12, 12, 0, DateTimeKind.Utc).ToLocalTime();
            c.DateTimeOffsetField = new DateTime(2008, 12, 12, 12, 12, 12, 0, DateTimeKind.Utc).ToLocalTime();
            c.PreField = "Pre";
            c.PostField = "Post";
            string json = JsonSerializer.Serialize(c);

            NullableDateTimeTestClass newOne = JsonSerializer.Deserialize<NullableDateTimeTestClass>(json);
            Assert.Equal(newOne.DateTimeField, c.DateTimeField);
            Assert.Equal(newOne.DateTimeOffsetField, c.DateTimeOffsetField);
            Assert.Equal(newOne.PostField, c.PostField);
            Assert.Equal(newOne.PreField, c.PreField);

            //test the other edge case too (start of a year)
            c.DateTimeField = new DateTime(2008, 1, 1, 1, 1, 1, 0, DateTimeKind.Utc).ToLocalTime();
            c.DateTimeOffsetField = new DateTime(2008, 1, 1, 1, 1, 1, 0, DateTimeKind.Utc).ToLocalTime();
            c.PreField = "Pre";
            c.PostField = "Post";
            json = JsonSerializer.Serialize(c);

            newOne = JsonSerializer.Deserialize<NullableDateTimeTestClass>(json);
            Assert.Equal(newOne.DateTimeField, c.DateTimeField);
            Assert.Equal(newOne.DateTimeOffsetField, c.DateTimeOffsetField);
            Assert.Equal(newOne.PostField, c.PostField);
            Assert.Equal(newOne.PreField, c.PreField);
        }

        [Fact]
        public void BlogCodeSample()
        {
            Person p = new Person
            {
                Name = "Keith",
                BirthDate = new DateTime(1980, 3, 8),
                LastModified = new DateTime(2009, 4, 12, 20, 44, 55),
            };

            string jsonText = JsonSerializer.Serialize(p, new JsonSerializerOptions { IgnoreNullValues = true});

            Assert.Equal(@"{""Name"":""Keith"",""BirthDate"":""1980-03-08T00:00:00"",""LastModified"":""2009-04-12T20:44:55""}", jsonText);
        }
    }

    internal class DateTimeTestClass
    {
        public string PreField { get; set; }

        public DateTime DateTimeField { get; set; }

        public DateTimeOffset DateTimeOffsetField { get; set; }
        public string PostField { get; set; }
    }

    internal class NullableDateTimeTestClass
    {
        public string PreField { get; set; }
        public DateTime? DateTimeField { get; set; }
        public DateTimeOffset? DateTimeOffsetField { get; set; }
        public string PostField { get; set; }
    }
}
