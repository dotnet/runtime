// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using Xunit;

namespace System.Json.Tests
{
    public class JsonPrimitiveTests
    {
        [Theory]
        [InlineData(1.1, "1.1")]
        [InlineData(-1.1, "-1.1")]
        [InlineData(1e-20, "1E-20")]
        [InlineData(1e+20, "1E+20")]
        [InlineData(1e-30, "1E-30")]
        [InlineData(1e+30, "1E+30")]
        [InlineData(double.NaN, "\"NaN\"")]
        [InlineData(double.PositiveInfinity, "\"Infinity\"")]
        [InlineData(double.NegativeInfinity, "\"-Infinity\"")]
        public void ToString_Double(double value, string expected)
        {
            ToString(new JsonPrimitive(value), expected);
        }

        [Theory]
        [InlineData(1.1, "1.1")]
        [InlineData(float.NaN, "\"NaN\"")]
        [InlineData(float.PositiveInfinity, "\"Infinity\"")]
        [InlineData(float.NegativeInfinity, "\"-Infinity\"")]
        public void ToString_Float(float value, string expected)
        {
            ToString(new JsonPrimitive(value), expected);
        }

        [Theory]
        [InlineData("abc", "\"abc\"")]
        public void ToString_String(string value, string expected)
        {
            ToString(new JsonPrimitive(value), expected);
        }

        [Fact]
        public void ToString_Null_WorksDependingOnOverload()
        {
            JsonPrimitive primitive = new JsonPrimitive((string)null);
            Assert.Equal("\"\"", primitive.ToString());

            using (StringWriter textWriter = new StringWriter())
            {
                primitive.Save(textWriter);
                Assert.Equal("\"\"", textWriter.ToString());
            }

            using (MemoryStream stream = new MemoryStream())
            {
                Assert.Throws<NullReferenceException>(() => primitive.Save(stream));
            }
        }

        [Theory]
        [InlineData('a', "\"a\"")]
        public void ToString_Char(char value, string expected)
        {
            ToString(new JsonPrimitive(value), expected);
        }

        [Fact]
        public void ToString_Bool()
        {
            ToString(new JsonPrimitive(true), "true");
            ToString(new JsonPrimitive(false), "false");
        }

        [Fact]
        public void ToString_DateTimeOffset()
        {
            ToString(new JsonPrimitive(DateTimeOffset.MinValue), DateTimeOffset.MinValue.ToString("G"));
        }

        [Fact]
        public void ToString_TimeSpan()
        {
            ToString(new JsonPrimitive(TimeSpan.Zero), TimeSpan.Zero.ToString("G"));
        }
        
        private void ToString(JsonValue primitive, string expected)
        {
            Assert.Equal(expected, primitive.ToString());

            using (StringWriter textWriter = new StringWriter())
            {
                primitive.Save(textWriter);
                Assert.Equal(expected, textWriter.ToString());
            }

            using (MemoryStream stream = new MemoryStream())
            {
                primitive.Save(stream);
                string result = Encoding.UTF8.GetString(stream.ToArray());
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void ToString_DateTime_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new JsonPrimitive(DateTime.MinValue).ToString());
        }

        [Fact]
        public void ToString_Guid_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => new JsonPrimitive(Guid.Empty).ToString());
        }

        [Fact]
        public void ToString_Uri_ThrowsInvalidCastException()
        {
            Assert.Throws<InvalidCastException>(() => new JsonPrimitive(new Uri("scheme://host/")).ToString());
        }

        [Fact]
        public void Save_InvalidJsonType_ThrowsInvalidOperationException()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                Assert.Throws<InvalidOperationException>(() => new JsonSubPrimitive().Save(stream));
            }
        }

        public class JsonSubPrimitive : JsonPrimitive
        {
            public JsonSubPrimitive() : base(true) { }

            public override JsonType JsonType => JsonType.Object;
        }
    }
}
