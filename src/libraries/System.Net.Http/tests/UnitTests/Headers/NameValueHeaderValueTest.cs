// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class NameValueHeaderValueTest
    {
        [Fact]
        public void Ctor_NameNull_Throw()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => { NameValueHeaderValue nameValue = new NameValueHeaderValue((string)null); });
        }

        [Fact]
        public void Ctor_NameEmpty_Throw()
        {
            // null and empty should be treated the same. So we also throw for empty strings.
            AssertExtensions.Throws<ArgumentException>("name", () => { NameValueHeaderValue nameValue = new NameValueHeaderValue(string.Empty); });
        }

        [Theory]
        [InlineData(" text ")]
        [InlineData("text ")]
        [InlineData(" text")]
        [InlineData("\ttext\t")]
        [InlineData("text\t")]
        [InlineData("\ttext")]
        [InlineData("te xt")]
        // The ctor takes a name which must not contain '='.
        [InlineData("te=xt")]
        [InlineData("te\u00E4xt")]
        public void Ctor_NameInvalidFormat_ThrowFormatException(string value)
        {
            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) are allowed.
            AssertFormatException(value, null);
        }

        [Theory]
        [InlineData("text", null)]
        [InlineData("host", "server.example.com:80")]
        [InlineData("quoted", "\"value\"")]
        [InlineData("empty", "")]
        public void Ctor_NameValidFormat_SuccessfullyCreated(string name, string value)
        {
            NameValueHeaderValue nameValue = new NameValueHeaderValue(name, value);
            Assert.Equal(name, nameValue.Name);
            Assert.Equal(value, nameValue.Value);
        }

        [Theory]
        [InlineData(" token ")]
        [InlineData("token ")]
        [InlineData(" token")]
        [InlineData("\ttoken")]
        [InlineData("\ttoken\t")]
        [InlineData("token\t")]
        [InlineData("\"quoted string with \" quotes\"")]
        [InlineData("\"quoted string with \"two\" quotes\"")]
        [InlineData("\"")]
        [InlineData(" ")]
        [InlineData("token\r")]
        [InlineData("\"token\r\"")]
        public void Ctor_ValueInvalidFormat_ThrowFormatException(string value)
        {
            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) are allowed.
            AssertFormatException("text", value);
        }

        [Fact]
        public void Ctor_ValueValidFormat_SuccessfullyCreated()
        {
            CheckValue(null);
            CheckValue(string.Empty);
            CheckValue("token_string");
            CheckValue("\"quoted string\"");
            CheckValue("\"quoted string with quoted \\\" quote-pair\"");
        }

        [Fact]
        public void Value_CallSetterWithInvalidValues_Throw()
        {
            // Just verify that the setter calls the same validation the ctor invokes.
            Assert.Throws<FormatException>(() => { var x = new NameValueHeaderValue("name"); x.Value = " x "; });
        }

        [Fact]
        public void ToString_UseNoValueAndTokenAndQuotedStringValues_SerializedCorrectly()
        {
            NameValueHeaderValue nameValue = new NameValueHeaderValue("text", "token");
            Assert.Equal("text=token", nameValue.ToString());

            nameValue.Value = "\"quoted string\"";
            Assert.Equal("text=\"quoted string\"", nameValue.ToString());

            nameValue.Value = null;
            Assert.Equal("text", nameValue.ToString());

            nameValue.Value = string.Empty;
            Assert.Equal("text", nameValue.ToString());
        }

        [Fact]
        public void GetHashCode_ValuesUseDifferentValues_HashDiffersAccordingToRfc()
        {
            NameValueHeaderValue nameValue1 = new NameValueHeaderValue("text");
            NameValueHeaderValue nameValue2 = new NameValueHeaderValue("text");

            nameValue1.Value = null;
            nameValue2.Value = null;
            Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue1.Value = "token";
            nameValue2.Value = null;
            Assert.NotEqual(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue1.Value = "token";
            nameValue2.Value = string.Empty;
            Assert.NotEqual(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue1.Value = null;
            nameValue2.Value = string.Empty;
            Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue1.Value = "token";
            nameValue2.Value = "TOKEN";
            Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue1.Value = "token";
            nameValue2.Value = "token";
            Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue1.Value = "\"quoted string\"";
            nameValue2.Value = "\"QUOTED STRING\"";
            Assert.NotEqual(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue1.Value = "\"quoted string\"";
            nameValue2.Value = "\"quoted string\"";
            Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_NameUseDifferentCasing_HashDiffersAccordingToRfc()
        {
            NameValueHeaderValue nameValue1 = new NameValueHeaderValue("text");
            NameValueHeaderValue nameValue2 = new NameValueHeaderValue("TEXT");
            Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());
        }

        [Fact]
        public void Equals_ValuesUseDifferentValues_ValuesAreEqualOrDifferentAccordingToRfc()
        {
            NameValueHeaderValue nameValue1 = new NameValueHeaderValue("text");
            NameValueHeaderValue nameValue2 = new NameValueHeaderValue("text");

            nameValue1.Value = null;
            nameValue2.Value = null;
            Assert.True(nameValue1.Equals(nameValue2), "<null> vs. <null>.");

            nameValue1.Value = "token";
            nameValue2.Value = null;
            Assert.False(nameValue1.Equals(nameValue2), "token vs. <null>.");

            nameValue1.Value = null;
            nameValue2.Value = "token";
            Assert.False(nameValue1.Equals(nameValue2), "<null> vs. token.");

            nameValue1.Value = string.Empty;
            nameValue2.Value = "token";
            Assert.False(nameValue1.Equals(nameValue2), "string.Empty vs. token.");

            nameValue1.Value = null;
            nameValue2.Value = string.Empty;
            Assert.True(nameValue1.Equals(nameValue2), "<null> vs. string.Empty.");

            nameValue1.Value = "token";
            nameValue2.Value = "TOKEN";
            Assert.True(nameValue1.Equals(nameValue2), "token vs. TOKEN.");

            nameValue1.Value = "token";
            nameValue2.Value = "token";
            Assert.True(nameValue1.Equals(nameValue2), "token vs. token.");

            nameValue1.Value = "\"quoted string\"";
            nameValue2.Value = "\"QUOTED STRING\"";
            Assert.False(nameValue1.Equals(nameValue2), "\"quoted string\" vs. \"QUOTED STRING\".");

            nameValue1.Value = "\"quoted string\"";
            nameValue2.Value = "\"quoted string\"";
            Assert.True(nameValue1.Equals(nameValue2), "\"quoted string\" vs. \"quoted string\".");

            Assert.False(nameValue1.Equals(null), "\"quoted string\" vs. <null>.");
        }

        [Fact]
        public void Equals_NameUseDifferentCasing_ConsideredEqual()
        {
            NameValueHeaderValue nameValue1 = new NameValueHeaderValue("text");
            NameValueHeaderValue nameValue2 = new NameValueHeaderValue("TEXT");
            Assert.True(nameValue1.Equals(nameValue2), "text vs. TEXT.");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            NameValueHeaderValue source = new NameValueHeaderValue("name", "value");
            NameValueHeaderValue clone = (NameValueHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Name, clone.Name);
            Assert.Equal(source.Value, clone.Value);
        }

        [Fact]
        public void GetNameValueLength_DifferentValidScenarios_AllReturnNonZero()
        {
            NameValueHeaderValue result = null;

            Assert.Equal(10, NameValueHeaderValue.GetNameValueLength("name=value", 0, DummyCreator, out result));
            Assert.Equal("name", result.Name);
            Assert.Equal("value", result.Value);

            Assert.Equal(10, NameValueHeaderValue.GetNameValueLength(" name=value", 1, DummyCreator, out result));
            Assert.Equal("name", result.Name);
            Assert.Equal("value", result.Value);

            Assert.Equal(4, NameValueHeaderValue.GetNameValueLength(" name", 1, DummyCreator, out result));
            Assert.Equal("name", result.Name);
            Assert.Null(result.Value);

            Assert.Equal(17, NameValueHeaderValue.GetNameValueLength("name=\"quoted str\"", 0, DummyCreator,
                out result));
            Assert.Equal("name", result.Name);
            Assert.Equal("\"quoted str\"", result.Value);

            Assert.Equal(17, NameValueHeaderValue.GetNameValueLength(" name=\"quoted str\"", 1, DummyCreator,
                out result));
            Assert.Equal("name", result.Name);
            Assert.Equal("\"quoted str\"", result.Value);

            Assert.Equal(12, NameValueHeaderValue.GetNameValueLength("name\t =va1ue\"", 0, DummyCreator, out result));
            Assert.Equal("name", result.Name);
            Assert.Equal("va1ue", result.Value);

            Assert.Equal(12, NameValueHeaderValue.GetNameValueLength(" name= va*ue ", 1, DummyCreator, out result));
            Assert.Equal("name", result.Name);
            Assert.Equal("va*ue", result.Value);

            Assert.Equal(6, NameValueHeaderValue.GetNameValueLength(" name  ", 1, DummyCreator, out result));
            Assert.Equal("name", result.Name);
            Assert.Null(result.Value);

            Assert.Equal(12, NameValueHeaderValue.GetNameValueLength(" name= va*ue ,", 1, DummyCreator, out result));
            Assert.Equal("name", result.Name);
            Assert.Equal("va*ue", result.Value);

            Assert.Equal(9, NameValueHeaderValue.GetNameValueLength(" name = va:ue", 1, DummyCreator, out result));
            Assert.Equal("name", result.Name);
            Assert.Equal("va", result.Value);
        }

        [Fact]
        public void GetNameValueLength_DifferentInvalidScenarios_AllReturnZero()
        {
            NameValueHeaderValue result = null;

            Assert.Equal(0, NameValueHeaderValue.GetNameValueLength(" name=value", 0, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, NameValueHeaderValue.GetNameValueLength(" name=", 1, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, NameValueHeaderValue.GetNameValueLength(" ,", 1, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, NameValueHeaderValue.GetNameValueLength("name=value", 10, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, NameValueHeaderValue.GetNameValueLength("", 0, DummyCreator, out result));
            Assert.Null(result);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            CheckValidParse("  name = value    ", new NameValueHeaderValue("name", "value"));
            CheckValidParse(" name", new NameValueHeaderValue("name"));
            CheckValidParse(" name=\"value\"", new NameValueHeaderValue("name", "\"value\""));
            CheckValidParse("name=value", new NameValueHeaderValue("name", "value"));
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("name[value");
            CheckInvalidParse("name=value=");
            CheckInvalidParse("name=\u4F1A");
            CheckInvalidParse("name==value");
            CheckInvalidParse("=value");
            CheckInvalidParse("name value");
            CheckInvalidParse("name=,value");
            CheckInvalidParse("\u4F1A");
            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  ,,");
            CheckInvalidParse(" , , name = value  ,  ");
            CheckInvalidParse(" name,");
            CheckInvalidParse(" ,name=\"value\"");
        }

        #region Helper methods

        private void CheckValidParse(string input, NameValueHeaderValue expectedResult)
        {
            NameValueHeaderValue result = NameValueHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(NameValueHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);

            // New lines are never allowed
            for (int i = 0; i < input.Length; i++)
            {
                CheckInvalidParse(input.Insert(i, "\r"));
                CheckInvalidParse(input.Insert(i, "\n"));
                CheckInvalidParse(input.Insert(i, "\r\n"));
                CheckInvalidParse(input.Insert(i, "\r\n "));
            }
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { NameValueHeaderValue.Parse(input); });

            Assert.False(NameValueHeaderValue.TryParse(input, out NameValueHeaderValue result));
            Assert.Null(result);
        }

        private static void CheckValue(string value)
        {
            NameValueHeaderValue nameValue = new NameValueHeaderValue("text", value);
            Assert.Equal(value, nameValue.Value);
        }

        private static void AssertFormatException(string name, string value)
        {
            Assert.Throws<FormatException>(() => { new NameValueHeaderValue(name, value); });
        }

        private static NameValueHeaderValue DummyCreator()
        {
            return new NameValueHeaderValue();
        }
        #endregion
    }
}
