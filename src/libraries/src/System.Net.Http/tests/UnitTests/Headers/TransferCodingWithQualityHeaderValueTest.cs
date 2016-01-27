// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using Xunit;

namespace System.Net.Http.Tests
{
    public class TransferCodingWithQualityHeaderValueTest
    {
        [Fact]
        public void Clone_Call_CloneFieldsMatchSourceFields()
        {
            // This test just verifies that TransferCodingWithQualityHeaderValue calls the correct base implementation.
            TransferCodingWithQualityHeaderValue source = new TransferCodingWithQualityHeaderValue("custom");
            TransferCodingWithQualityHeaderValue clone = (TransferCodingWithQualityHeaderValue)
                ((ICloneable)source).Clone();
            Assert.Equal(source.Value, clone.Value);
            Assert.Equal(0, clone.Parameters.Count);

            source.Quality = 0.1;
            clone = (TransferCodingWithQualityHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Value, clone.Value);
            Assert.Equal(0.1, clone.Quality);
            Assert.Equal(1, clone.Parameters.Count);

            source.Parameters.Add(new NameValueHeaderValue("custom", "customValue"));
            clone = (TransferCodingWithQualityHeaderValue)((ICloneable)source).Clone();
            Assert.Equal(source.Value, clone.Value);
            Assert.Equal(0.1, clone.Quality);
            Assert.Equal(2, clone.Parameters.Count);
            Assert.Equal("custom", clone.Parameters.ElementAt(1).Name);
            Assert.Equal("customValue", clone.Parameters.ElementAt(1).Value);
        }

        [Fact]
        public void Ctor_AddValueAndQuality_QualityParameterAdded()
        {
            TransferCodingWithQualityHeaderValue mediaType = new TransferCodingWithQualityHeaderValue("custom", 0.08);
            Assert.Equal(0.08, mediaType.Quality);
            Assert.Equal("custom", mediaType.Value);
            Assert.Equal(1, mediaType.Parameters.Count);
        }

        [Fact]
        public void Parse_SetOfValidValueStrings_ParsedCorrectly()
        {
            TransferCodingWithQualityHeaderValue expected = new TransferCodingWithQualityHeaderValue("custom");
            CheckValidParse("\r\n custom  ", expected);
            CheckValidParse("custom", expected);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a 
            // transfer-coding parser.
            expected.Parameters.Add(new NameValueHeaderValue("name", "value"));
            CheckValidParse("\r\n custom ;  name =   value ", expected);
            CheckValidParse("  custom;name=value", expected);
            CheckValidParse("  custom ; name=value", expected);


            TransferCodingWithQualityHeaderValue value1 = new TransferCodingWithQualityHeaderValue("custom1");
            value1.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            value1.Quality = 1.0;

            CheckValidParse("custom1 ; param1 =value1 ; q= 1.0 ", value1);

            TransferCodingWithQualityHeaderValue value2 = new TransferCodingWithQualityHeaderValue("custom2");
            value2.Parameters.Add(new NameValueHeaderValue("param2", "value2"));
            value2.Quality = 0.5;

            CheckValidParse("\r\n custom2; param2= value2; q =0.5  ", value2);
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

            CheckInvalidParse(",custom1; param1=value1; q=1.0,,\r\n custom2; param2=value2; q=0.5  ,");
            CheckInvalidParse("custom1; param1=value1; q=1.0,");
            CheckInvalidParse(",\r\n custom2; param2=value2; q=0.5");

            CheckInvalidParse(null);
            CheckInvalidParse(string.Empty);
            CheckInvalidParse("  ");
            CheckInvalidParse("  ,,");
        }

        [Fact]
        public void TryParse_SetOfValidValueStrings_ParsedCorrectly()
        {
            TransferCodingWithQualityHeaderValue expected = new TransferCodingWithQualityHeaderValue("custom");
            CheckValidTryParse("\r\n custom  ", expected);
            CheckValidTryParse("custom", expected);

            // We don't have to test all possible input strings, since most of the pieces are handled by other parsers.
            // The purpose of this test is to verify that these other parsers are combined correctly to build a 
            // transfer-coding parser.
            expected.Parameters.Add(new NameValueHeaderValue("name", "value"));
            CheckValidTryParse("\r\n custom ;  name =   value ", expected);
            CheckValidTryParse("  custom;name=value", expected);
            CheckValidTryParse("  custom ; name=value", expected);


            TransferCodingWithQualityHeaderValue value1 = new TransferCodingWithQualityHeaderValue("custom1");
            value1.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            value1.Quality = 1.0;

            CheckValidTryParse("custom1 ; param1 =value1 ; q= 1.0 ", value1);

            TransferCodingWithQualityHeaderValue value2 = new TransferCodingWithQualityHeaderValue("custom2");
            value2.Parameters.Add(new NameValueHeaderValue("param2", "value2"));
            value2.Quality = 0.5;

            CheckValidTryParse("\r\n custom2; param2= value2; q =0.5  ", value2);
        }

        [Fact]
        public void TryParse_SetOfInvalidValueStrings_ReturnsFalse()
        {
            CheckInvalidTryParse("custom; name=value;");
            CheckInvalidTryParse("custom; name1=value1; name2=value2;");
            CheckInvalidTryParse(",,custom");
            CheckInvalidTryParse(" , , custom");
            CheckInvalidTryParse("\r\n custom  , chunked");
            CheckInvalidTryParse("\r\n custom  , , , chunked");
            CheckInvalidTryParse("custom , \u4F1A");
            CheckInvalidTryParse("\r\n , , custom ;  name =   value ");

            CheckInvalidTryParse(",custom1; param1=value1; q=1.0,,\r\n custom2; param2=value2; q=0.5  ,");
            CheckInvalidTryParse("custom1; param1=value1; q=1.0,");
            CheckInvalidTryParse(",\r\n custom2; param2=value2; q=0.5");

            CheckInvalidTryParse(null);
            CheckInvalidTryParse(string.Empty);
            CheckInvalidTryParse("  ");
            CheckInvalidTryParse("  ,,");
        }

        #region Helper methods

        private void CheckValidParse(string input, TransferCodingWithQualityHeaderValue expectedResult)
        {
            TransferCodingWithQualityHeaderValue result = TransferCodingWithQualityHeaderValue.Parse(input);
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidParse(string input)
        {
            Assert.Throws<FormatException>(() => { TransferCodingWithQualityHeaderValue.Parse(input); });
        }

        private void CheckValidTryParse(string input, TransferCodingWithQualityHeaderValue expectedResult)
        {
            TransferCodingWithQualityHeaderValue result = null;
            Assert.True(TransferCodingWithQualityHeaderValue.TryParse(input, out result));
            Assert.Equal(expectedResult, result);
        }

        private void CheckInvalidTryParse(string input)
        {
            TransferCodingWithQualityHeaderValue result = null;
            Assert.False(TransferCodingWithQualityHeaderValue.TryParse(input, out result));
            Assert.Null(result);
        }
        #endregion
    }
}
