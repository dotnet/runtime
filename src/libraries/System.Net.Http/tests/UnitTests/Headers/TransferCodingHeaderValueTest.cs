// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class TransferCodingHeaderValueTest
    {
        [Fact]
        public void Ctor_ValueNull_Throw()
        {
            AssertExtensions.Throws<ArgumentNullException>("value", () => { new TransferCodingHeaderValue(null); });
        }

        [Fact]
        public void Ctor_ValueEmpty_Throw()
        {
            // null and empty should be treated the same. So we also throw for empty strings.
            AssertExtensions.Throws<ArgumentException>("value", () => { new TransferCodingHeaderValue(string.Empty); });
        }

        [Fact]
        public void Ctor_TransferCodingInvalidFormat_ThrowFormatException()
        {
            // When adding values using strongly typed objects, no leading/trailing LWS (whitespace) are allowed.
            AssertFormatException(" custom ");
            AssertFormatException("custom;");
            AssertFormatException("ch??nked");
            AssertFormatException("\"chunked\"");
            AssertFormatException("custom; name=value");
        }

        [Fact]
        public void Ctor_TransferCodingValidFormat_SuccessfullyCreated()
        {
            TransferCodingHeaderValue transferCoding = new TransferCodingHeaderValue("custom");
            Assert.Equal("custom", transferCoding.Value);
            Assert.Equal(0, transferCoding.Parameters.Count);
        }

        [Fact]
        public void Parameters_AddNull_Throw()
        {
            TransferCodingHeaderValue transferCoding = new TransferCodingHeaderValue("custom");

            Assert.Throws<ArgumentNullException>(() => { transferCoding.Parameters.Add(null); });
        }

        [Fact]
        public void ToString_UseDifferentTransferCodings_AllSerializedCorrectly()
        {
            TransferCodingHeaderValue transferCoding = new TransferCodingHeaderValue("custom");
            Assert.Equal("custom", transferCoding.ToString());

            transferCoding.Parameters.Add(new NameValueHeaderValue("paramName", "\"param value\""));
            Assert.Equal("custom; paramName=\"param value\"", transferCoding.ToString());

            transferCoding.Parameters.Add(new NameValueHeaderValue("paramName2", "\"param value2\""));
            Assert.Equal("custom; paramName=\"param value\"; paramName2=\"param value2\"",
                transferCoding.ToString());
        }

        [Fact]
        public void GetHashCode_UseTransferCodingWithAndWithoutParameters_SameOrDifferentHashCodes()
        {
            TransferCodingHeaderValue transferCoding1 = new TransferCodingHeaderValue("custom");
            TransferCodingHeaderValue transferCoding2 = new TransferCodingHeaderValue("CUSTOM");
            TransferCodingHeaderValue transferCoding3 = new TransferCodingHeaderValue("custom");
            transferCoding3.Parameters.Add(new NameValueHeaderValue("name", "value"));
            TransferCodingHeaderValue transferCoding4 = new TransferCodingHeaderValue("custom");
            transferCoding4.Parameters.Add(new NameValueHeaderValue("NAME", "VALUE"));
            TransferCodingHeaderValue transferCoding5 = new TransferCodingHeaderValue("custom");
            transferCoding5.Parameters.Add(new NameValueHeaderValue("name", "\"value\""));
            TransferCodingHeaderValue transferCoding6 = new TransferCodingHeaderValue("custom");
            transferCoding6.Parameters.Add(new NameValueHeaderValue("name", "\"VALUE\""));
            TransferCodingHeaderValue transferCoding7 = new TransferCodingHeaderValue("custom");
            transferCoding7.Parameters.Add(new NameValueHeaderValue("name", "\"VALUE\""));
            transferCoding7.Parameters.Clear();

            Assert.Equal(transferCoding1.GetHashCode(), transferCoding2.GetHashCode());
            Assert.NotEqual(transferCoding1.GetHashCode(), transferCoding3.GetHashCode());
            Assert.Equal(transferCoding3.GetHashCode(), transferCoding4.GetHashCode());
            Assert.NotEqual(transferCoding5.GetHashCode(), transferCoding6.GetHashCode());
            Assert.Equal(transferCoding1.GetHashCode(), transferCoding7.GetHashCode());
        }

        [Fact]
        public void Equals_UseTransferCodingWithAndWithoutParameters_EqualOrNotEqualNoExceptions()
        {
            TransferCodingHeaderValue transferCoding1 = new TransferCodingHeaderValue("custom");
            TransferCodingHeaderValue transferCoding2 = new TransferCodingHeaderValue("CUSTOM");
            TransferCodingHeaderValue transferCoding3 = new TransferCodingHeaderValue("custom");
            transferCoding3.Parameters.Add(new NameValueHeaderValue("name", "value"));
            TransferCodingHeaderValue transferCoding4 = new TransferCodingHeaderValue("custom");
            transferCoding4.Parameters.Add(new NameValueHeaderValue("NAME", "VALUE"));
            TransferCodingHeaderValue transferCoding5 = new TransferCodingHeaderValue("custom");
            transferCoding5.Parameters.Add(new NameValueHeaderValue("name", "\"value\""));
            TransferCodingHeaderValue transferCoding6 = new TransferCodingHeaderValue("custom");
            transferCoding6.Parameters.Add(new NameValueHeaderValue("name", "\"VALUE\""));
            TransferCodingHeaderValue transferCoding7 = new TransferCodingHeaderValue("custom");
            transferCoding7.Parameters.Add(new NameValueHeaderValue("name", "\"VALUE\""));
            transferCoding7.Parameters.Clear();

            Assert.False(transferCoding1.Equals(null), "Compare to <null>.");
            Assert.True(transferCoding1.Equals(transferCoding2), "Different casing.");
            Assert.False(transferCoding1.Equals(transferCoding3), "No params vs. custom param.");
            Assert.True(transferCoding3.Equals(transferCoding4), "Params have different casing.");
            Assert.False(transferCoding5.Equals(transferCoding6),
                "Param value are quoted strings with different casing.");
            Assert.True(transferCoding1.Equals(transferCoding7), "no vs. empty parameters collection.");
        }

        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            TransferCodingHeaderValue source = new TransferCodingHeaderValue("custom");
            TransferCodingHeaderValue clone = (TransferCodingHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Value, clone.Value);
            Assert.Equal(0, clone.Parameters.Count);

            source.Parameters.Add(new NameValueHeaderValue("custom", "customValue"));
            clone = (TransferCodingHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Value, clone.Value);
            Assert.Equal(1, clone.Parameters.Count);
            Assert.Equal("custom", clone.Parameters.ElementAt(0).Name);
            Assert.Equal("customValue", clone.Parameters.ElementAt(0).Value);
        }

        [Fact]
        public void GetTransferCodingLength_DifferentValidScenarios_AllReturnNonZero()
        {
            TransferCodingHeaderValue result = null;

            Assert.Equal(7, TransferCodingHeaderValue.GetTransferCodingLength("chunked", 0,
                DummyCreator, out result));
            Assert.Equal("chunked", result.Value);
            Assert.Equal(0, result.Parameters.Count);

            Assert.Equal(5, TransferCodingHeaderValue.GetTransferCodingLength("gzip , chunked", 0,
                DummyCreator, out result));
            Assert.Equal("gzip", result.Value);
            Assert.Equal(0, result.Parameters.Count);

            Assert.Equal(18, TransferCodingHeaderValue.GetTransferCodingLength("custom; name=value", 0,
                DummyCreator, out result));
            Assert.Equal("custom", result.Value);
            Assert.Equal(1, result.Parameters.Count);
            Assert.Equal("name", result.Parameters.ElementAt(0).Name);
            Assert.Equal("value", result.Parameters.ElementAt(0).Value);

            // Note that TransferCodingHeaderValue recognizes the first transfer-coding as valid, even though it is
            // followed by an invalid character. The parser will call GetTransferCodingLength() starting at the invalid
            // character which will result in GetTransferCodingLength() returning 0 (see next test).
            Assert.Equal(26, TransferCodingHeaderValue.GetTransferCodingLength(
                " custom;name1=value1;name2 ,  \u4F1A", 1, DummyCreator, out result));
            Assert.Equal("custom", result.Value);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal("name1", result.Parameters.ElementAt(0).Name);
            Assert.Equal("value1", result.Parameters.ElementAt(0).Value);
            Assert.Equal("name2", result.Parameters.ElementAt(1).Name);
            Assert.Null(result.Parameters.ElementAt(1).Value);

            // There will be no exception for invalid characters. GetTransferCodingLength() will just return a length
            // of 0. The caller needs to validate if that's OK or not.
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength("\u4F1A", 0, DummyCreator, out result));

            Assert.Equal(43, TransferCodingHeaderValue.GetTransferCodingLength(
                "  custom ; name1 = \"value1\" ; name2 = value2 , next", 2, DummyCreator, out result));
            Assert.Equal("custom", result.Value);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal("name1", result.Parameters.ElementAt(0).Name);
            Assert.Equal("\"value1\"", result.Parameters.ElementAt(0).Value);
            Assert.Equal("name2", result.Parameters.ElementAt(1).Name);
            Assert.Equal("value2", result.Parameters.ElementAt(1).Value);

            Assert.Equal(32, TransferCodingHeaderValue.GetTransferCodingLength(
                " custom;name1=value1;name2=value2,next", 1, DummyCreator, out result));
            Assert.Equal("custom", result.Value);
            Assert.Equal(2, result.Parameters.Count);
            Assert.Equal("name1", result.Parameters.ElementAt(0).Name);
            Assert.Equal("value1", result.Parameters.ElementAt(0).Value);
            Assert.Equal("name2", result.Parameters.ElementAt(1).Name);
            Assert.Equal("value2", result.Parameters.ElementAt(1).Value);
        }

        [Fact]
        public void GetTransferCodingLength_DifferentInvalidScenarios_AllReturnZero()
        {
            TransferCodingHeaderValue result = null;

            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength(" custom", 0, DummyCreator,
                out result));
            Assert.Null(result);
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength("custom;", 0, DummyCreator,
                out result));
            Assert.Null(result);
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength("custom;name=", 0, DummyCreator,
                out result));
            Assert.Null(result);
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength("custom;name=value;", 0,
                DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength("custom;name=,value;", 0,
                DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength("custom;", 0, DummyCreator,
                out result));
            Assert.Null(result);
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength(null, 0, DummyCreator, out result));
            Assert.Null(result);
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength(string.Empty, 0, DummyCreator,
                out result));
            Assert.Null(result);
            Assert.Equal(0, TransferCodingHeaderValue.GetTransferCodingLength("\r\nchunked", 0, DummyCreator,
                out result));
            Assert.Null(result);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            TransferCodingHeaderValue expected = new TransferCodingHeaderValue("custom");
            CheckValidParse(" custom  ", expected);
            CheckValidParse("custom", expected);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a
            // transfer-coding parser.
            expected.Parameters.Add(new NameValueHeaderValue("name", "value"));
            CheckValidParse(" custom ;  name =   value ", expected);
            CheckValidParse("  custom;name=value", expected);
            CheckValidParse("  custom ; name=value", expected);
        }

        [Fact]
        public void Parse_SetOfInvalidValueStrings_Throws()
        {
            CheckInvalidParse("custom; name=value;");
            CheckInvalidParse("custom; name1=value1; name2=value2;");
            CheckInvalidParse(",,custom");
            CheckInvalidParse(" , , custom");
            CheckInvalidParse("\r\n custom  , chunked");
            CheckInvalidParse("\r\n custom  , , , chunked");
            CheckInvalidParse("custom , \u4F1A");
            CheckInvalidParse("\r\n , , custom ;  name =   value ");
            CheckInvalidParse(" custom ; \r\n name =   value ");

            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  ,,");
        }

        #region Helper methods

        private void CheckValidParse(string input, TransferCodingHeaderValue expectedResult)
        {
            TransferCodingHeaderValue result = TransferCodingHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);

            Assert.True(TransferCodingHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { TransferCodingHeaderValue.Parse(input); });

            Assert.False(TransferCodingHeaderValue.TryParse(input, out TransferCodingHeaderValue result));
            Assert.Null(result);
        }

        private static void AssertFormatException(string transferCoding)
        {
            Assert.Throws<FormatException>(() => { new TransferCodingHeaderValue(transferCoding); });
        }

        private static TransferCodingHeaderValue DummyCreator()
        {
            return new TransferCodingHeaderValue();
        }
        #endregion
    }
}
