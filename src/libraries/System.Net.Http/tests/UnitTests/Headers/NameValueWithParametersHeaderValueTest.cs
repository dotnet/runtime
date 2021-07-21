// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class NameValueWithParametersHeaderValueTest
    {
        [Fact]
        public void Ctor_NameNull_Throw()
        {
            AssertExtensions.Throws<ArgumentException>("name", () => { NameValueWithParametersHeaderValue nameValue = new NameValueWithParametersHeaderValue(null); });
        }

        [Fact]
        public void Ctor_NameEmpty_Throw()
        {
            // null and empty should be treated the same. So we also throw for empty strings.
            AssertExtensions.Throws<ArgumentException>("name", () => { NameValueWithParametersHeaderValue nameValue = new NameValueWithParametersHeaderValue(string.Empty); });
        }

        [Fact]
        public void Ctor_CallBaseCtor_Success()
        {
            // Just make sure the base ctor gets called correctly. Validation of input parameters is done in the base
            // class.
            NameValueWithParametersHeaderValue nameValue = new NameValueWithParametersHeaderValue("name");
            Assert.Equal("name", nameValue.Name);
            Assert.Null(nameValue.Value);

            nameValue = new NameValueWithParametersHeaderValue("name", "value");
            Assert.Equal("name", nameValue.Name);
            Assert.Equal("value", nameValue.Value);
        }

        [Fact]
        public void Parameters_AddNull_Throw()
        {
            NameValueWithParametersHeaderValue nameValue = new NameValueWithParametersHeaderValue("name");

            Assert.Throws<ArgumentNullException>(() => { nameValue.Parameters.Add(null); });
        }

        [Fact]
        public void ToString_WithAndWithoutParameters_SerializedCorrectly()
        {
            NameValueWithParametersHeaderValue nameValue = new NameValueWithParametersHeaderValue("text", "token");
            Assert.Equal("text=token", nameValue.ToString());

            nameValue.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            nameValue.Parameters.Add(new NameValueHeaderValue("param2", "value2"));
            Assert.Equal("text=token; param1=value1; param2=value2", nameValue.ToString());
        }

        [Fact]
        public void GetHashCode_ValuesUseDifferentValues_HashDiffersAccordingToRfc()
        {
            NameValueWithParametersHeaderValue nameValue1 = new NameValueWithParametersHeaderValue("text");
            NameValueWithParametersHeaderValue nameValue2 = new NameValueWithParametersHeaderValue("text");

            // NameValueWithParametersHeaderValue just calls methods of the base class. Just verify Parameters is used.
            Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue1.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            nameValue2.Value = null;
            Assert.NotEqual(nameValue1.GetHashCode(), nameValue2.GetHashCode());

            nameValue2.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            Assert.Equal(nameValue1.GetHashCode(), nameValue2.GetHashCode());
        }

        [Fact]
        public void Equals_ValuesUseDifferentValues_ValuesAreEqualOrDifferentAccordingToRfc()
        {
            NameValueWithParametersHeaderValue nameValue1 = new NameValueWithParametersHeaderValue("text", "value");
            NameValueWithParametersHeaderValue nameValue2 = new NameValueWithParametersHeaderValue("text", "value");
            NameValueHeaderValue nameValue3 = new NameValueHeaderValue("text", "value");

            // NameValueWithParametersHeaderValue just calls methods of the base class. Just verify Parameters is used.
            Assert.True(nameValue1.Equals(nameValue2), "No parameters.");
            Assert.False(nameValue1.Equals(null), "Compare to null.");
            Assert.False(nameValue1.Equals(nameValue3), "Compare to base class instance.");

            nameValue1.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            Assert.False(nameValue1.Equals(nameValue2), "none vs. 1 parameter.");

            nameValue2.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            Assert.True(nameValue1.Equals(nameValue2), "1 parameter vs. 1 parameter.");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            NameValueWithParametersHeaderValue source = new NameValueWithParametersHeaderValue("name", "value");
            source.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            NameValueWithParametersHeaderValue clone = (NameValueWithParametersHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Name, clone.Name);
            Assert.Equal(source.Value, clone.Value);
            Assert.Equal(1, clone.Parameters.Count);
            Assert.Equal("param1", clone.Parameters.First().Name);
            Assert.Equal("value1", clone.Parameters.First().Value);
        }

        [Fact]
        public void GetNameValueLength_DifferentScenariosWithNoParameters_AllReturnNonZero()
        {
            NameValueWithParametersHeaderValue result = null;

            CallGetNameValueWithParametersLength("name=value", 0, 10, out result);
            Assert.Equal("name", result.Name);
            Assert.Equal("value", result.Value);

            CallGetNameValueWithParametersLength(" name=value", 1, 10, out result);
            Assert.Equal("name", result.Name);
            Assert.Equal("value", result.Value);

            CallGetNameValueWithParametersLength(" name", 1, 4, out result);
            Assert.Equal("name", result.Name);
            Assert.Null(result.Value);

            CallGetNameValueWithParametersLength("name=\"quoted str\"", 0, 17, out result);
            Assert.Equal("name", result.Name);
            Assert.Equal("\"quoted str\"", result.Value);

            CallGetNameValueWithParametersLength(" name=\"quoted str\"", 1, 17, out result);
            Assert.Equal("name", result.Name);
            Assert.Equal("\"quoted str\"", result.Value);

            CallGetNameValueWithParametersLength("name\t =va1ue\"", 0, 12, out result);
            Assert.Equal("name", result.Name);
            Assert.Equal("va1ue", result.Value);

            CallGetNameValueWithParametersLength(" name  ", 1, 6, out result);
            Assert.Equal("name", result.Name);
            Assert.Null(result.Value);
        }

        [Fact]
        public void GetNameValueLength_DifferentScenariosWithParameters_AllReturnNonZero()
        {
            NameValueWithParametersHeaderValue result = null;

            CallGetNameValueWithParametersLength(" name = value ; param1 = value1 ,", 1, 31, out result);
            Assert.Equal("name", result.Name);
            Assert.Equal("value", result.Value);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("param1", result.Parameters.First().Name);
            Assert.Equal("value1", result.Parameters.First().Value);

            CallGetNameValueWithParametersLength(" name=value;param1=value1;param2=value2,next", 1, 38, out result);
            Assert.Equal("name", result.Name);
            Assert.Equal("value", result.Value);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal("param1", result.Parameters.ElementAt(0).Name);
            Assert.Equal("value1", result.Parameters.ElementAt(0).Value);
            Assert.Equal("param2", result.Parameters.ElementAt(1).Name);
            Assert.Equal("value2", result.Parameters.ElementAt(1).Value);

            CallGetNameValueWithParametersLength(" name= value ;   param1 , next", 1, 23, out result);
            Assert.Equal("name", result.Name);
            Assert.Equal("value", result.Value);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("param1", result.Parameters.First().Name);
            Assert.Null(result.Parameters.First().Value);

            CallGetNameValueWithParametersLength(" name ;   param1 , next", 1, 16, out result);
            Assert.Equal("name", result.Name);
            Assert.Null(result.Value);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("param1", result.Parameters.First().Name);
            Assert.Null(result.Parameters.First().Value);
        }

        [Fact]
        public void GetNameValueLength_DifferentInvalidScenarios_AllReturnZero()
        {
            CheckInvalidGetNameValueWithParametersLength(" name=value", 0);
            CheckInvalidGetNameValueWithParametersLength(" name=", 1);
            CheckInvalidGetNameValueWithParametersLength(" name=value; param;", 1);
            CheckInvalidGetNameValueWithParametersLength(" name;", 1);
            CheckInvalidGetNameValueWithParametersLength(" ,name", 1);
            CheckInvalidGetNameValueWithParametersLength("name=value", 10);
            CheckInvalidGetNameValueWithParametersLength("", 0);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            NameValueWithParametersHeaderValue expected = new NameValueWithParametersHeaderValue("custom");
            CheckValidParse(" custom  ", expected);
            CheckValidParse("custom", expected);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a
            // transfer-coding parser.
            expected.Parameters.Add(new NameValueHeaderValue("name", "value"));
            CheckValidParse(" custom ;  name =   value ", expected);
            CheckValidParse("  custom;name=value", expected);
            CheckValidParse("  custom ; name=value", expected);

            expected.Parameters.Add(new NameValueHeaderValue("foo", "bar"));
            CheckValidParse("custom; name=value; foo=bar", expected);
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("custom\n");
            CheckInvalidParse("custom\u4F1A");
            CheckInvalidParse("custom; name=value;");
            CheckInvalidParse("custom; name1=value1; name2=value2;");
            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  ,,");
        }

        #region Helper methods

        private void CheckValidParse(string input, NameValueWithParametersHeaderValue expectedResult)
        {
            NameValueWithParametersHeaderValue result = NameValueWithParametersHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(NameValueWithParametersHeaderValue.TryParse(input, out result));
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
            Assert.Throws<FormatException>(() => { NameValueWithParametersHeaderValue.Parse(input); });

            Assert.False(NameValueWithParametersHeaderValue.TryParse(input, out NameValueWithParametersHeaderValue result));
            Assert.Null(result);
        }

        private static void CallGetNameValueWithParametersLength(string input, int startIndex, int expectedLength,
            out NameValueWithParametersHeaderValue result)
        {
            object temp = null;
            Assert.Equal(expectedLength, NameValueWithParametersHeaderValue.GetNameValueWithParametersLength(input,
                startIndex, out temp));
            result = temp as NameValueWithParametersHeaderValue;
        }

        private static void CheckInvalidGetNameValueWithParametersLength(string input, int startIndex)
        {
            object result = null;
            Assert.Equal(0, NameValueWithParametersHeaderValue.GetNameValueWithParametersLength(input, startIndex,
                out result));
            Assert.Null(result);
        }
        #endregion
    }
}
