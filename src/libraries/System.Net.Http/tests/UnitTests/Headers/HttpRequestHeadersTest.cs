// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpRequestHeadersTest
    {
        private HttpRequestHeaders headers;

        public HttpRequestHeadersTest()
        {
            headers = new HttpRequestHeaders();
        }

        #region Request headers

        [Fact]
        public void Accept_AddInvalidValueUsingUnusualCasing_ParserRetrievedUsingCaseInsensitiveComparison()
        {
            // Use uppercase header name to make sure the parser gets retrieved using case-insensitive comparison.
            Assert.Throws<FormatException>(() => { headers.Add("AcCePt", "this is invalid"); });
        }

        [Fact]
        public void Accept_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            MediaTypeWithQualityHeaderValue value1 = new MediaTypeWithQualityHeaderValue("text/plain");
            value1.CharSet = "utf-8";
            value1.Quality = 0.5;
            value1.Parameters.Add(new NameValueHeaderValue("custom", "value"));
            MediaTypeWithQualityHeaderValue value2 = new MediaTypeWithQualityHeaderValue("text/plain");
            value2.CharSet = "iso-8859-1";
            value2.Quality = 0.3868;

            Assert.Equal(0, headers.Accept.Count);

            headers.Accept.Add(value1);
            headers.Accept.Add(value2);

            Assert.Equal(2, headers.Accept.Count);
            Assert.Equal(value1, headers.Accept.ElementAt(0));
            Assert.Equal(value2, headers.Accept.ElementAt(1));

            headers.Accept.Clear();
            Assert.Equal(0, headers.Accept.Count);
        }

        [Fact]
        public void Accept_ReadEmptyProperty_EmptyCollection()
        {
            HttpRequestMessage request = new HttpRequestMessage();

            Assert.Equal(0, request.Headers.Accept.Count);
            // Copy to another list
            List<MediaTypeWithQualityHeaderValue> accepts = request.Headers.Accept.ToList();
            Assert.Equal(0, accepts.Count);
            accepts = new List<MediaTypeWithQualityHeaderValue>(request.Headers.Accept);
            Assert.Equal(0, accepts.Count);
        }

        [Fact]
        public void Accept_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Accept",
                ",, , ,,text/plain; charset=iso-8859-1; q=1.0, */xml; charset=utf-8; q=0.5,,,");

            MediaTypeWithQualityHeaderValue value1 = new MediaTypeWithQualityHeaderValue("text/plain");
            value1.CharSet = "iso-8859-1";
            value1.Quality = 1.0;

            MediaTypeWithQualityHeaderValue value2 = new MediaTypeWithQualityHeaderValue("*/xml");
            value2.CharSet = "utf-8";
            value2.Quality = 0.5;

            Assert.Equal(value1, headers.Accept.ElementAt(0));
            Assert.Equal(value2, headers.Accept.ElementAt(1));
        }

        [Fact]
        public void Accept_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            // Add a valid media-type with an invalid quality value
            headers.TryAddWithoutValidation("Accept", "text/plain; q=a"); // invalid quality
            Assert.NotNull(headers.Accept.First());
            Assert.Null(headers.Accept.First().Quality);
            Assert.Equal("text/plain; q=a", headers.Accept.First().ToString());

            headers.Clear();
            headers.TryAddWithoutValidation("Accept", "text/plain application/xml"); // no separator

            Assert.Equal(0, headers.Accept.Count);
            Assert.Equal(1, headers.GetValues("Accept").Count());
            Assert.Equal("text/plain application/xml", headers.GetValues("Accept").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Accept", "text/plain; charset=iso-8859-1; q=1.0,\r\n */xml; charset=utf-8; q=0.5,,,");

            Assert.Equal(0, headers.Accept.Count);
            Assert.False(headers.Contains("Accept"));
        }

        [Fact]
        public void AcceptCharset_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.AcceptCharset.Count);

            headers.AcceptCharset.Add(new StringWithQualityHeaderValue("iso-8859-5"));
            headers.AcceptCharset.Add(new StringWithQualityHeaderValue("unicode-1-1", 0.8));

            Assert.Equal(2, headers.AcceptCharset.Count);
            Assert.Equal(new StringWithQualityHeaderValue("iso-8859-5"), headers.AcceptCharset.ElementAt(0));
            Assert.Equal(new StringWithQualityHeaderValue("unicode-1-1", 0.8), headers.AcceptCharset.ElementAt(1));

            headers.AcceptCharset.Clear();
            Assert.Equal(0, headers.AcceptCharset.Count);
        }

        [Fact]
        public void AcceptCharset_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Accept-Charset", ", ,,iso-8859-5 , utf-8 ; q=0.300 ,,,");

            Assert.Equal(new StringWithQualityHeaderValue("iso-8859-5"),
                headers.AcceptCharset.ElementAt(0));
            Assert.Equal(new StringWithQualityHeaderValue("utf-8", 0.3),
                headers.AcceptCharset.ElementAt(1));
        }

        [Fact]
        public void AcceptCharset_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Accept-Charset", "iso-8859-5 utf-8"); // no separator
            Assert.Equal(0, headers.AcceptCharset.Count);
            Assert.Equal(1, headers.GetValues("Accept-Charset").Count());
            Assert.Equal("iso-8859-5 utf-8", headers.GetValues("Accept-Charset").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Accept-Charset", "utf-8; q=1; q=0.3");
            Assert.Equal(0, headers.AcceptCharset.Count);
            Assert.Equal(1, headers.GetValues("Accept-Charset").Count());
            Assert.Equal("utf-8; q=1; q=0.3", headers.GetValues("Accept-Charset").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Accept-Charset", "iso-8859-5, \r\n utf-8; q=0.300");
            Assert.Equal(0, headers.AcceptCharset.Count);
            Assert.False(headers.Contains("Accept-Charset"));
        }

        [Fact]
        public void AcceptCharset_AddMultipleValuesAndGetValueString_AllValuesAddedUsingTheCorrectDelimiter()
        {
            headers.TryAddWithoutValidation("Accept-Charset", "invalid value");
            headers.Add("Accept-Charset", "utf-8");
            headers.AcceptCharset.Add(new StringWithQualityHeaderValue("iso-8859-5", 0.5));

            foreach (var header in headers.NonValidated)
            {
                Assert.Equal("Accept-Charset", header.Key);
                Assert.Equal("invalid value, utf-8, iso-8859-5; q=0.5", header.Value.ToString());
            }
        }

        [Fact]
        public void AcceptEncoding_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.AcceptEncoding.Count);

            headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("compress", 0.9));
            headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));

            Assert.Equal(2, headers.AcceptEncoding.Count);
            Assert.Equal(new StringWithQualityHeaderValue("compress", 0.9), headers.AcceptEncoding.ElementAt(0));
            Assert.Equal(new StringWithQualityHeaderValue("gzip"), headers.AcceptEncoding.ElementAt(1));

            headers.AcceptEncoding.Clear();
            Assert.Equal(0, headers.AcceptEncoding.Count);
        }

        [Fact]
        public void AcceptEncoding_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Accept-Encoding", ", gzip; q=1.0, identity; q=0.5, *;q=0, ");

            Assert.Equal(new StringWithQualityHeaderValue("gzip", 1),
                headers.AcceptEncoding.ElementAt(0));
            Assert.Equal(new StringWithQualityHeaderValue("identity", 0.5),
                headers.AcceptEncoding.ElementAt(1));
            Assert.Equal(new StringWithQualityHeaderValue("*", 0),
                headers.AcceptEncoding.ElementAt(2));

            headers.AcceptEncoding.Clear();
            headers.TryAddWithoutValidation("Accept-Encoding", "");
            Assert.Equal(0, headers.AcceptEncoding.Count);
            Assert.False(headers.Contains("Accept-Encoding"));
        }

        [Fact]
        public void AcceptEncoding_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Accept-Encoding", "gzip deflate"); // no separator
            Assert.Equal(0, headers.AcceptEncoding.Count);
            Assert.Equal(1, headers.GetValues("Accept-Encoding").Count());
            Assert.Equal("gzip deflate", headers.GetValues("Accept-Encoding").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Accept-Encoding", "compress; q=1; gzip");
            Assert.Equal(0, headers.AcceptEncoding.Count);
            Assert.Equal(1, headers.GetValues("Accept-Encoding").Count());
            Assert.Equal("compress; q=1; gzip", headers.GetValues("Accept-Encoding").First());
        }

        [Fact]
        public void AcceptLanguage_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.AcceptLanguage.Count);

            headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("da"));
            headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-GB", 0.8));
            headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.7));

            Assert.Equal(3, headers.AcceptLanguage.Count);
            Assert.Equal(new StringWithQualityHeaderValue("da"), headers.AcceptLanguage.ElementAt(0));
            Assert.Equal(new StringWithQualityHeaderValue("en-GB", 0.8), headers.AcceptLanguage.ElementAt(1));
            Assert.Equal(new StringWithQualityHeaderValue("en", 0.7), headers.AcceptLanguage.ElementAt(2));

            headers.AcceptLanguage.Clear();
            Assert.Equal(0, headers.AcceptLanguage.Count);
        }

        [Fact]
        public void AcceptLanguage_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Accept-Language", " , de-DE;q=0.9,de-AT;q=0.5,*;q=0.010 , ");

            Assert.Equal(new StringWithQualityHeaderValue("de-DE", 0.9),
                headers.AcceptLanguage.ElementAt(0));
            Assert.Equal(new StringWithQualityHeaderValue("de-AT", 0.5),
                headers.AcceptLanguage.ElementAt(1));
            Assert.Equal(new StringWithQualityHeaderValue("*", 0.01),
                headers.AcceptLanguage.ElementAt(2));

            headers.AcceptLanguage.Clear();
            headers.TryAddWithoutValidation("Accept-Language", "");
            Assert.Equal(0, headers.AcceptLanguage.Count);
            Assert.False(headers.Contains("Accept-Language"));
        }

        [Fact]
        public void AcceptLanguage_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Accept-Language", "de -DE"); // no separator
            Assert.Equal(0, headers.AcceptLanguage.Count);
            Assert.Equal(1, headers.GetValues("Accept-Language").Count());
            Assert.Equal("de -DE", headers.GetValues("Accept-Language").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Accept-Language", "en; q=0.4,[");
            Assert.Equal(0, headers.AcceptLanguage.Count);
            Assert.Equal(1, headers.GetValues("Accept-Language").Count());
            Assert.Equal("en; q=0.4,[", headers.GetValues("Accept-Language").First());
        }

        [Fact]
        public void Expect_Add100Continue_Success()
        {
            // use non-default casing to make sure we do case-insensitive comparison.
            headers.Expect.Add(new NameValueWithParametersHeaderValue("100-CONTINUE"));
            Assert.True(headers.ExpectContinue == true);
            Assert.Equal(1, headers.Expect.Count);
        }

        [Fact]
        public void Expect_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.Expect.Count);
            Assert.Null(headers.ExpectContinue);

            headers.Expect.Add(new NameValueWithParametersHeaderValue("custom1"));
            headers.Expect.Add(new NameValueWithParametersHeaderValue("custom2"));
            headers.ExpectContinue = true;

            // Connection collection has 2 values plus '100-Continue'
            Assert.Equal(3, headers.Expect.Count);
            Assert.Equal(3, headers.GetValues("Expect").Count());
            Assert.True(headers.ExpectContinue == true, "ExpectContinue == true");
            Assert.Equal(new NameValueWithParametersHeaderValue("custom1"), headers.Expect.ElementAt(0));
            Assert.Equal(new NameValueWithParametersHeaderValue("custom2"), headers.Expect.ElementAt(1));

            // Remove '100-continue' value from store. But leave other 'Expect' values.
            headers.ExpectContinue = false;
            Assert.True(headers.ExpectContinue == false, "ExpectContinue == false");
            Assert.Equal(2, headers.Expect.Count);
            Assert.Equal(new NameValueWithParametersHeaderValue("custom1"), headers.Expect.ElementAt(0));
            Assert.Equal(new NameValueWithParametersHeaderValue("custom2"), headers.Expect.ElementAt(1));

            headers.ExpectContinue = true;
            headers.Expect.Clear();
            Assert.True(headers.ExpectContinue == false, "ExpectContinue should be modified by Expect.Clear().");
            Assert.Equal(0, headers.Expect.Count);
            IEnumerable<string> dummyArray;
            Assert.False(headers.TryGetValues("Expect", out dummyArray), "Expect header count after Expect.Clear().");

            // Remove '100-continue' value from store. Since there are no other 'Expect' values, remove whole header.
            headers.ExpectContinue = false;
            Assert.True(headers.ExpectContinue == false, "ExpectContinue == false");
            Assert.Equal(0, headers.Expect.Count);
            Assert.False(headers.Contains("Expect"));

            headers.ExpectContinue = null;
            Assert.Null(headers.ExpectContinue);
        }

        [Fact]
        public void Expect_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Expect",
                ", , 100-continue, name1 = value1, name2; param2=paramValue2, name3=value3; param3 ,");

            // Connection collection has 3 values plus '100-continue'
            Assert.Equal(4, headers.Expect.Count);
            Assert.Equal(4, headers.GetValues("Expect").Count());
            Assert.True(headers.ExpectContinue == true, "ExpectContinue expected to be true.");

            Assert.Equal(new NameValueWithParametersHeaderValue("100-continue"),
                headers.Expect.ElementAt(0));

            Assert.Equal(new NameValueWithParametersHeaderValue("name1", "value1"),
                headers.Expect.ElementAt(1));

            NameValueWithParametersHeaderValue expected2 = new NameValueWithParametersHeaderValue("name2");
            expected2.Parameters.Add(new NameValueHeaderValue("param2", "paramValue2"));
            Assert.Equal(expected2, headers.Expect.ElementAt(2));

            NameValueWithParametersHeaderValue expected3 = new NameValueWithParametersHeaderValue("name3", "value3");
            expected3.Parameters.Add(new NameValueHeaderValue("param3"));
            Assert.Equal(expected3, headers.Expect.ElementAt(3));

            headers.Expect.Clear();
            Assert.Null(headers.ExpectContinue);
            Assert.Equal(0, headers.Expect.Count);
            IEnumerable<string> dummyArray;
            Assert.False(headers.TryGetValues("Expect", out dummyArray), "Expect header count after Expect.Clear().");
        }

        [Fact]
        public void Expect_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Expect", "100-continue other"); // no separator

            Assert.Equal(0, headers.Expect.Count);
            Assert.Equal(1, headers.GetValues("Expect").Count());
            Assert.Equal("100-continue other", headers.GetValues("Expect").First());
        }

        [Fact]
        public void Host_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.Host);

            headers.Host = "host";
            Assert.Equal("host", headers.Host);

            headers.Host = null;
            Assert.Null(headers.Host);
            Assert.False(headers.Contains("Host"),
                "Header store should not contain a header 'Host' after setting it to null.");

            Assert.Throws<FormatException>(() => { headers.Host = "invalid host"; });
        }

        [Fact]
        public void Host_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Host", "host:80");

            Assert.Equal("host:80", headers.Host);
        }

        [Fact]
        public void IfMatch_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.IfMatch.Count);

            headers.IfMatch.Add(new EntityTagHeaderValue("\"custom1\""));
            headers.IfMatch.Add(new EntityTagHeaderValue("\"custom2\"", true));

            Assert.Equal(2, headers.IfMatch.Count);
            Assert.Equal(2, headers.GetValues("If-Match").Count());
            Assert.Equal(new EntityTagHeaderValue("\"custom1\""), headers.IfMatch.ElementAt(0));
            Assert.Equal(new EntityTagHeaderValue("\"custom2\"", true), headers.IfMatch.ElementAt(1));

            headers.IfMatch.Clear();
            Assert.Equal(0, headers.IfMatch.Count);
            Assert.False(headers.Contains("If-Match"), "Header store should not contain 'If-Match'");
        }

        [Fact]
        public void IfMatch_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("If-Match", ", , W/\"tag1\", \"tag2\", W/\"tag3\" ,");

            Assert.Equal(3, headers.IfMatch.Count);
            Assert.Equal(3, headers.GetValues("If-Match").Count());

            Assert.Equal(new EntityTagHeaderValue("\"tag1\"", true), headers.IfMatch.ElementAt(0));
            Assert.Equal(new EntityTagHeaderValue("\"tag2\"", false), headers.IfMatch.ElementAt(1));
            Assert.Equal(new EntityTagHeaderValue("\"tag3\"", true), headers.IfMatch.ElementAt(2));

            headers.IfMatch.Clear();
            headers.Add("If-Match", "*");
            Assert.Equal(1, headers.IfMatch.Count);
            Assert.Same(EntityTagHeaderValue.Any, headers.IfMatch.ElementAt(0));
        }

        [Fact]
        public void IfMatch_UseAddMethodWithInvalidInput_PropertyNotUpdated()
        {
            headers.TryAddWithoutValidation("If-Match", "W/\"tag1\" \"tag2\""); // no separator
            Assert.Equal(0, headers.IfMatch.Count);
            Assert.Equal(1, headers.GetValues("If-Match").Count());
        }

        [Fact]
        public void IfNoneMatch_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.IfNoneMatch.Count);

            headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"custom1\""));
            headers.IfNoneMatch.Add(new EntityTagHeaderValue("\"custom2\"", true));

            Assert.Equal(2, headers.IfNoneMatch.Count);
            Assert.Equal(2, headers.GetValues("If-None-Match").Count());
            Assert.Equal(new EntityTagHeaderValue("\"custom1\""), headers.IfNoneMatch.ElementAt(0));
            Assert.Equal(new EntityTagHeaderValue("\"custom2\"", true), headers.IfNoneMatch.ElementAt(1));

            headers.IfNoneMatch.Clear();
            Assert.Equal(0, headers.IfNoneMatch.Count);
            Assert.False(headers.Contains("If-None-Match"), "Header store should not contain 'If-None-Match'");
        }

        [Fact]
        public void IfNoneMatch_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("If-None-Match", "W/\"tag1\", \"tag2\", W/\"tag3\"");

            Assert.Equal(3, headers.IfNoneMatch.Count);
            Assert.Equal(3, headers.GetValues("If-None-Match").Count());

            Assert.Equal(new EntityTagHeaderValue("\"tag1\"", true), headers.IfNoneMatch.ElementAt(0));
            Assert.Equal(new EntityTagHeaderValue("\"tag2\"", false), headers.IfNoneMatch.ElementAt(1));
            Assert.Equal(new EntityTagHeaderValue("\"tag3\"", true), headers.IfNoneMatch.ElementAt(2));

            headers.IfNoneMatch.Clear();
            headers.Add("If-None-Match", "*");
            Assert.Equal(1, headers.IfNoneMatch.Count);
            Assert.Same(EntityTagHeaderValue.Any, headers.IfNoneMatch.ElementAt(0));
        }

        [Fact]
        public void TE_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            TransferCodingWithQualityHeaderValue value1 = new TransferCodingWithQualityHeaderValue("custom");
            value1.Quality = 0.5;
            value1.Parameters.Add(new NameValueHeaderValue("name", "value"));
            TransferCodingWithQualityHeaderValue value2 = new TransferCodingWithQualityHeaderValue("custom");
            value2.Quality = 0.3868;

            Assert.Equal(0, headers.TE.Count);

            headers.TE.Add(value1);
            headers.TE.Add(value2);

            Assert.Equal(2, headers.TE.Count);
            Assert.Equal(value1, headers.TE.ElementAt(0));
            Assert.Equal(value2, headers.TE.ElementAt(1));

            headers.TE.Clear();
            Assert.Equal(0, headers.TE.Count);
        }

        [Fact]
        public void TE_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("TE",
                ",custom1; param1=value1; q=1.0,, custom2; param2=value2; q=0.5  ,");

            TransferCodingWithQualityHeaderValue value1 = new TransferCodingWithQualityHeaderValue("custom1");
            value1.Parameters.Add(new NameValueHeaderValue("param1", "value1"));
            value1.Quality = 1.0;

            TransferCodingWithQualityHeaderValue value2 = new TransferCodingWithQualityHeaderValue("custom2");
            value2.Parameters.Add(new NameValueHeaderValue("param2", "value2"));
            value2.Quality = 0.5;

            Assert.Equal(value1, headers.TE.ElementAt(0));
            Assert.Equal(value2, headers.TE.ElementAt(1));

            headers.Clear();
            headers.TryAddWithoutValidation("TE", "");
            Assert.False(headers.Contains("TE"), "'TE' header should not be added if it just has empty values.");
        }

        [Fact]
        public void Range_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.Range);
            RangeHeaderValue value = new RangeHeaderValue(1, 2);

            headers.Range = value;
            Assert.Equal(value, headers.Range);

            headers.Range = null;
            Assert.Null(headers.Range);
        }

        [Fact]
        public void Range_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Range", "custom= , ,1-2, -4 , ");

            RangeHeaderValue value = new RangeHeaderValue();
            value.Unit = "custom";
            value.Ranges.Add(new RangeItemHeaderValue(1, 2));
            value.Ranges.Add(new RangeItemHeaderValue(null, 4));

            Assert.Equal(value, headers.Range);
        }

        [Fact]
        public void Authorization_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.Authorization);

            headers.Authorization = new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ==");
            Assert.Equal(new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ=="), headers.Authorization);

            headers.Authorization = null;
            Assert.Null(headers.Authorization);
            Assert.False(headers.Contains("Authorization"),
                "Header store should not contain a header 'Authorization' after setting it to null.");
        }

        [Fact]
        public void Authorization_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Authorization", "NTLM blob");

            Assert.Equal(new AuthenticationHeaderValue("NTLM", "blob"), headers.Authorization);
        }

        [Fact]
        public void ProxyAuthorization_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.ProxyAuthorization);

            headers.ProxyAuthorization = new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ==");
            Assert.Equal(new AuthenticationHeaderValue("Basic", "QWxhZGRpbjpvcGVuIHNlc2FtZQ=="),
                headers.ProxyAuthorization);

            headers.ProxyAuthorization = null;
            Assert.Null(headers.ProxyAuthorization);
            Assert.False(headers.Contains("ProxyAuthorization"),
                "Header store should not contain a header 'ProxyAuthorization' after setting it to null.");
        }

        [Fact]
        public void ProxyAuthorization_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Proxy-Authorization", "NTLM blob");

            Assert.Equal(new AuthenticationHeaderValue("NTLM", "blob"), headers.ProxyAuthorization);
        }

        [Fact]
        public void UserAgent_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.UserAgent.Count);

            headers.UserAgent.Add(new ProductInfoHeaderValue("(custom1)"));
            headers.UserAgent.Add(new ProductInfoHeaderValue("custom2", "1.1"));

            Assert.Equal(2, headers.UserAgent.Count);
            Assert.Equal(2, headers.GetValues("User-Agent").Count());
            Assert.Equal(new ProductInfoHeaderValue("(custom1)"), headers.UserAgent.ElementAt(0));
            Assert.Equal(new ProductInfoHeaderValue("custom2", "1.1"), headers.UserAgent.ElementAt(1));

            headers.UserAgent.Clear();
            Assert.Equal(0, headers.UserAgent.Count);
            Assert.False(headers.Contains("User-Agent"), "User-Agent header should be removed after calling Clear().");

            headers.UserAgent.Add(new ProductInfoHeaderValue("(comment)"));
            headers.UserAgent.Remove(new ProductInfoHeaderValue("(comment)"));
            Assert.Equal(0, headers.UserAgent.Count);
            Assert.False(headers.Contains("User-Agent"), "User-Agent header should be removed after removing last value.");
        }

        [Fact]
        public void UserAgent_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("User-Agent", "Opera/9.80 (Windows NT 6.1; U; en) Presto/2.6.30 Version/10.63");

            Assert.Equal(4, headers.UserAgent.Count);
            Assert.Equal(4, headers.GetValues("User-Agent").Count());

            Assert.Equal(new ProductInfoHeaderValue("Opera", "9.80"), headers.UserAgent.ElementAt(0));
            Assert.Equal(new ProductInfoHeaderValue("(Windows NT 6.1; U; en)"), headers.UserAgent.ElementAt(1));
            Assert.Equal(new ProductInfoHeaderValue("Presto", "2.6.30"), headers.UserAgent.ElementAt(2));
            Assert.Equal(new ProductInfoHeaderValue("Version", "10.63"), headers.UserAgent.ElementAt(3));

            headers.UserAgent.Clear();
            Assert.Equal(0, headers.UserAgent.Count);
            Assert.False(headers.Contains("User-Agent"), "User-Agent header should be removed after calling Clear().");
        }

        [Fact]
        public void UserAgent_TryGetValuesAndGetValues_Malformed()
        {
            string malformedUserAgent = "Mozilla/4.0 (compatible (compatible; MSIE 8.0; Windows NT 6.1; Trident/7.0)";
            headers.TryAddWithoutValidation("User-Agent", malformedUserAgent);
            Assert.True(headers.TryGetValues("User-Agent", out IEnumerable<string> ua));
            Assert.Equal(1, ua.Count());
            Assert.Equal(malformedUserAgent, ua.First());
            Assert.Equal(malformedUserAgent, headers.GetValues("User-Agent").First());
        }

        [Fact]
        public void UserAgent_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("User-Agent", "custom\u4F1A");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.UserAgent.Descriptor));
            Assert.Equal(1, headers.GetValues("User-Agent").Count());
            Assert.Equal("custom\u4F1A", headers.GetValues("User-Agent").First());

            headers.Clear();
            // Note that "User-Agent" uses whitespace as separators, so the following is an invalid value
            headers.TryAddWithoutValidation("User-Agent", "custom1, custom2");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.UserAgent.Descriptor));
            Assert.Equal(1, headers.GetValues("User-Agent").Count());
            Assert.Equal("custom1, custom2", headers.GetValues("User-Agent").First());

            headers.Clear();
            headers.TryAddWithoutValidation("User-Agent", "custom1, ");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.UserAgent.Descriptor));
            Assert.Equal(1, headers.GetValues("User-Agent").Count());
            Assert.Equal("custom1, ", headers.GetValues("User-Agent").First());

            headers.Clear();
            headers.TryAddWithoutValidation("User-Agent", ",custom1");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.UserAgent.Descriptor));
            Assert.Equal(1, headers.GetValues("User-Agent").Count());
            Assert.Equal(",custom1", headers.GetValues("User-Agent").First());
        }

        [Fact]
        public void UserAgent_AddMultipleValuesAndGetValueString_AllValuesAddedUsingTheCorrectDelimiter()
        {
            headers.TryAddWithoutValidation("User-Agent", "custom\u4F1A");
            headers.Add("User-Agent", "custom2/1.1");
            headers.UserAgent.Add(new ProductInfoHeaderValue("(comment)"));

            foreach (var header in headers.NonValidated)
            {
                Assert.Equal("User-Agent", header.Key);
                Assert.Equal("custom\u4F1A custom2/1.1 (comment)", header.Value.ToString());
            }
        }

        [Fact]
        public void IfRange_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.IfRange);

            headers.IfRange = new RangeConditionHeaderValue("\"x\"");

            Assert.Equal(1, headers.GetValues("If-Range").Count());
            Assert.Equal(new RangeConditionHeaderValue("\"x\""), headers.IfRange);

            headers.IfRange = null;
            Assert.Null(headers.IfRange);
            Assert.False(headers.Contains("If-Range"), "If-Range header should be removed after calling Clear().");
        }

        [Fact]
        public void IfRange_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("If-Range", " W/\"tag\" ");

            Assert.Equal(new RangeConditionHeaderValue(new EntityTagHeaderValue("\"tag\"", true)),
                headers.IfRange);
            Assert.Equal(1, headers.GetValues("If-Range").Count());

            headers.IfRange = null;
            Assert.Null(headers.IfRange);
            Assert.False(headers.Contains("If-Range"), "If-Range header should be removed after calling Clear().");
        }

        [Fact]
        public void IfRange_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("If-Range", "\"tag\"\u4F1A");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.IfRange.Descriptor));
            Assert.Equal(1, headers.GetValues("If-Range").Count());
            Assert.Equal("\"tag\"\u4F1A", headers.GetValues("If-Range").First());

            headers.Clear();
            headers.TryAddWithoutValidation("If-Range", " \"tag\", ");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.IfRange.Descriptor));
            Assert.Equal(1, headers.GetValues("If-Range").Count());
            Assert.Equal(" \"tag\", ", headers.GetValues("If-Range").First());
        }

        [Fact]
        public void From_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.From);

            headers.From = "info@example.com";
            Assert.Equal("info@example.com", headers.From);

            headers.From = null;
            Assert.Null(headers.From);
            Assert.False(headers.Contains("From"),
                "Header store should not contain a header 'From' after setting it to null.");

            // values are not validated, so invalid values are accepted
            headers.From = " ";
            Assert.Equal(" ", headers.From);

            headers.From = "invalid email address";
            Assert.Equal("invalid email address", headers.From);

            // Null and empty string are equivalent. Setting to empty means remove the From header value (if any).
            headers.From = string.Empty;
            Assert.Null(headers.From);
            Assert.False(headers.Contains("From"),
                "Header store should not contain a header 'From' after setting it to string.Empty.");
        }

        [Fact]
        public void From_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("From", "  \"My Name\"   info@example.com  ");
            Assert.Equal("\"My Name\"   info@example.com  ", headers.From);

            // The following encoded string represents the character sequence "\u4F1A\u5458\u670D\u52A1".
            headers.Clear();
            headers.TryAddWithoutValidation("From", "=?utf-8?Q?=E4=BC=9A=E5=91=98=E6=9C=8D=E5=8A=A1?= <info@example.com>");
            Assert.Equal("=?utf-8?Q?=E4=BC=9A=E5=91=98=E6=9C=8D=E5=8A=A1?= <info@example.com>", headers.From);
        }

        [Fact]
        public void From_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            // values are not validated, so invalid values are accepted
            headers.TryAddWithoutValidation("From", " info@example.com ,");
            Assert.Equal("info@example.com ,", headers.GetSingleParsedValue(KnownHeaders.From.Descriptor));
            Assert.Equal(1, headers.GetValues("From").Count());
            Assert.Equal("info@example.com ,", headers.GetValues("From").First());

            headers.Clear();
            headers.TryAddWithoutValidation("From", "info@");
            Assert.Equal("info@", headers.GetSingleParsedValue(KnownHeaders.From.Descriptor));
            Assert.Equal(1, headers.GetValues("From").Count());
            Assert.Equal("info@", headers.GetValues("From").First());
        }

        [Fact]
        public void From_ValueContainsNewLineCharacters_Throws()
        {
            Assert.Throws<FormatException>(() => headers.From = "Foo\r\nBar");
            Assert.Throws<FormatException>(() => headers.Add("From", "Foo\r\nBar"));
        }

        [Fact]
        public void IfModifiedSince_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.IfModifiedSince);

            DateTimeOffset expected = DateTimeOffset.Now;
            headers.IfModifiedSince = expected;
            Assert.Equal(expected, headers.IfModifiedSince);

            headers.IfModifiedSince = null;
            Assert.Null(headers.IfModifiedSince);
            Assert.False(headers.Contains("If-Modified-Since"),
                "Header store should not contain a header 'IfModifiedSince' after setting it to null.");
        }

        [Fact]
        public void IfModifiedSince_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("If-Modified-Since", "  Sun, 06 Nov 1994 08:49:37 GMT  ");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), headers.IfModifiedSince);

            headers.Clear();
            headers.TryAddWithoutValidation("If-Modified-Since", "Sun, 06 Nov 1994 08:49:37 GMT");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), headers.IfModifiedSince);
        }

        [Fact]
        public void IfModifiedSince_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("If-Modified-Since", " Sun, 06 Nov 1994 08:49:37 GMT ,");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.IfModifiedSince.Descriptor));
            Assert.Equal(1, headers.GetValues("If-Modified-Since").Count());
            Assert.Equal(" Sun, 06 Nov 1994 08:49:37 GMT ,", headers.GetValues("If-Modified-Since").First());

            headers.Clear();
            headers.TryAddWithoutValidation("If-Modified-Since", " Sun, 06 Nov ");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.IfModifiedSince.Descriptor));
            Assert.Equal(1, headers.GetValues("If-Modified-Since").Count());
            Assert.Equal(" Sun, 06 Nov ", headers.GetValues("If-Modified-Since").First());
        }

        [Fact]
        public void IfUnmodifiedSince_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.IfUnmodifiedSince);

            DateTimeOffset expected = DateTimeOffset.Now;
            headers.IfUnmodifiedSince = expected;
            Assert.Equal(expected, headers.IfUnmodifiedSince);

            headers.IfUnmodifiedSince = null;
            Assert.Null(headers.IfUnmodifiedSince);
            Assert.False(headers.Contains("If-Unmodified-Since"),
                "Header store should not contain a header 'IfUnmodifiedSince' after setting it to null.");
        }

        [Fact]
        public void IfUnmodifiedSince_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("If-Unmodified-Since", "  Sun, 06 Nov 1994 08:49:37 GMT  ");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), headers.IfUnmodifiedSince);

            headers.Clear();
            headers.TryAddWithoutValidation("If-Unmodified-Since", "Sun, 06 Nov 1994 08:49:37 GMT");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), headers.IfUnmodifiedSince);
        }

        [Fact]
        public void IfUnmodifiedSince_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("If-Unmodified-Since", " Sun, 06 Nov 1994 08:49:37 GMT ,");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.IfUnmodifiedSince.Descriptor));
            Assert.Equal(1, headers.GetValues("If-Unmodified-Since").Count());
            Assert.Equal(" Sun, 06 Nov 1994 08:49:37 GMT ,", headers.GetValues("If-Unmodified-Since").First());

            headers.Clear();
            headers.TryAddWithoutValidation("If-Unmodified-Since", " Sun, 06 Nov ");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.IfUnmodifiedSince.Descriptor));
            Assert.Equal(1, headers.GetValues("If-Unmodified-Since").Count());
            Assert.Equal(" Sun, 06 Nov ", headers.GetValues("If-Unmodified-Since").First());
        }

        [Fact]
        public void Referrer_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.Referrer);

            Uri expected = new Uri("http://example.com/path/");
            headers.Referrer = expected;
            Assert.Equal(expected, headers.Referrer);

            headers.Referrer = null;
            Assert.Null(headers.Referrer);
            Assert.False(headers.Contains("Referer"),
                "Header store should not contain a header 'Referrer' after setting it to null.");
        }

        [Fact]
        public void Referrer_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Referer", "  http://www.example.com/path/?q=v  ");
            Assert.Equal(new Uri("http://www.example.com/path/?q=v"), headers.Referrer);

            headers.Clear();
            headers.TryAddWithoutValidation("Referer", "/relative/uri/");
            Assert.Equal(new Uri("/relative/uri/", UriKind.Relative), headers.Referrer);
        }

        [Fact]
        public void Referrer_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Referer", " http://example.com http://other");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.Referer.Descriptor));
            Assert.Equal(1, headers.GetValues("Referer").Count());
            Assert.Equal(" http://example.com http://other", headers.GetValues("Referer").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Referer", "http://host /other");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.Referer.Descriptor));
            Assert.Equal(1, headers.GetValues("Referer").Count());
            Assert.Equal("http://host /other", headers.GetValues("Referer").First());
        }

        [Fact]
        public void MaxForwards_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.MaxForwards);

            headers.MaxForwards = 15;
            Assert.Equal(15, headers.MaxForwards);

            headers.MaxForwards = null;
            Assert.Null(headers.MaxForwards);
            Assert.False(headers.Contains("Max-Forwards"),
                "Header store should not contain a header 'MaxForwards' after setting it to null.");

            // Make sure the header gets serialized correctly
            headers.MaxForwards = 12345;
            Assert.Equal("12345", headers.GetValues("Max-Forwards").First());
        }

        [Fact]
        public void MaxForwards_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Max-Forwards", "  00123  ");
            Assert.Equal(123, headers.MaxForwards);

            headers.Clear();
            headers.TryAddWithoutValidation("Max-Forwards", "0");
            Assert.Equal(0, headers.MaxForwards);
        }

        [Fact]
        public void MaxForwards_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Max-Forwards", "15,");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.MaxForwards.Descriptor));
            Assert.Equal(1, headers.GetValues("Max-Forwards").Count());
            Assert.Equal("15,", headers.GetValues("Max-Forwards").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Max-Forwards", "1.0");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.MaxForwards.Descriptor));
            Assert.Equal(1, headers.GetValues("Max-Forwards").Count());
            Assert.Equal("1.0", headers.GetValues("Max-Forwards").First());
        }

        [Fact]
        public void AddHeaders_SpecialHeaderValuesOnSourceNotOnDestination_Copied()
        {
            // Positive
            HttpRequestHeaders source = new HttpRequestHeaders();
            source.ExpectContinue = true;
            source.TransferEncodingChunked = true;
            source.ConnectionClose = true;

            HttpRequestHeaders destination = new HttpRequestHeaders();
            Assert.Null(destination.ExpectContinue);
            Assert.Null(destination.TransferEncodingChunked);
            Assert.Null(destination.ConnectionClose);

            destination.AddHeaders(source);
            Assert.NotNull(destination.ExpectContinue);
            Assert.NotNull(destination.TransferEncodingChunked);
            Assert.NotNull(destination.ConnectionClose);
            Assert.True(destination.ExpectContinue.Value);
            Assert.True(destination.TransferEncodingChunked.Value);
            Assert.True(destination.ConnectionClose.Value);

            // Negative
            source = new HttpRequestHeaders();
            source.ExpectContinue = false;
            source.TransferEncodingChunked = false;
            source.ConnectionClose = false;

            destination = new HttpRequestHeaders();
            Assert.Null(destination.ExpectContinue);
            Assert.Null(destination.TransferEncodingChunked);
            Assert.Null(destination.ConnectionClose);

            destination.AddHeaders(source);
            Assert.NotNull(destination.ExpectContinue);
            Assert.NotNull(destination.TransferEncodingChunked);
            Assert.NotNull(destination.ConnectionClose);
            Assert.False(destination.ExpectContinue.Value);
            Assert.False(destination.TransferEncodingChunked.Value);
            Assert.False(destination.ConnectionClose.Value);
        }

        [Fact]
        public void AddHeaders_SpecialHeaderValuesOnDestinationNotOnSource_NotCopied()
        {
            // Positive
            HttpRequestHeaders destination = new HttpRequestHeaders();
            destination.ExpectContinue = true;
            destination.TransferEncodingChunked = true;
            destination.ConnectionClose = true;
            Assert.NotNull(destination.ExpectContinue);
            Assert.NotNull(destination.TransferEncodingChunked);
            Assert.NotNull(destination.ConnectionClose);
            Assert.True(destination.ExpectContinue.Value);
            Assert.True(destination.TransferEncodingChunked.Value);
            Assert.True(destination.ConnectionClose.Value);

            HttpRequestHeaders source = new HttpRequestHeaders();
            Assert.Null(source.ExpectContinue);
            Assert.Null(source.TransferEncodingChunked);
            Assert.Null(source.ConnectionClose);

            destination.AddHeaders(source);
            Assert.Null(source.ExpectContinue);
            Assert.Null(source.TransferEncodingChunked);
            Assert.Null(source.ConnectionClose);
            Assert.NotNull(destination.ExpectContinue);
            Assert.NotNull(destination.TransferEncodingChunked);
            Assert.NotNull(destination.ConnectionClose);
            Assert.True(destination.ExpectContinue.Value);
            Assert.True(destination.TransferEncodingChunked.Value);
            Assert.True(destination.ConnectionClose.Value);

            // Negative
            destination = new HttpRequestHeaders();
            destination.ExpectContinue = false;
            destination.TransferEncodingChunked = false;
            destination.ConnectionClose = false;
            Assert.NotNull(destination.ExpectContinue);
            Assert.NotNull(destination.TransferEncodingChunked);
            Assert.NotNull(destination.ConnectionClose);
            Assert.False(destination.ExpectContinue.Value);
            Assert.False(destination.TransferEncodingChunked.Value);
            Assert.False(destination.ConnectionClose.Value);

            source = new HttpRequestHeaders();
            Assert.Null(source.ExpectContinue);
            Assert.Null(source.TransferEncodingChunked);
            Assert.Null(source.ConnectionClose);

            destination.AddHeaders(source);
            Assert.Null(source.ExpectContinue);
            Assert.Null(source.TransferEncodingChunked);
            Assert.Null(source.ConnectionClose);
            Assert.NotNull(destination.ExpectContinue);
            Assert.NotNull(destination.TransferEncodingChunked);
            Assert.NotNull(destination.ConnectionClose);
            Assert.False(destination.ExpectContinue.Value);
            Assert.False(destination.TransferEncodingChunked.Value);
            Assert.False(destination.ConnectionClose.Value);
        }

        #endregion

        #region General headers

        [Fact]
        public void Connection_AddClose_Success()
        {
            headers.Connection.Add("CLOSE"); // use non-default casing to make sure we do case-insensitive comparison.
            Assert.True(headers.ConnectionClose == true);
            Assert.Equal(1, headers.Connection.Count);
        }

        [Fact]
        public void Connection_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.Connection.Count);
            Assert.Null(headers.ConnectionClose);

            headers.Connection.Add("custom1");
            headers.Connection.Add("custom2");
            headers.ConnectionClose = true;

            // Connection collection has 2 values plus 'close'
            Assert.Equal(3, headers.Connection.Count);
            Assert.Equal(3, headers.GetValues("Connection").Count());
            Assert.True(headers.ConnectionClose == true, "ConnectionClose");

            Assert.Equal("custom1", headers.Connection.ElementAt(0));
            Assert.Equal("custom2", headers.Connection.ElementAt(1));

            // Remove 'close' value from store. But leave other 'Connection' values.
            headers.ConnectionClose = false;
            Assert.True(headers.ConnectionClose == false, "ConnectionClose == false");
            Assert.Equal(2, headers.Connection.Count);
            Assert.Equal("custom1", headers.Connection.ElementAt(0));
            Assert.Equal("custom2", headers.Connection.ElementAt(1));

            headers.ConnectionClose = true;
            headers.Connection.Clear();
            Assert.True(headers.ConnectionClose == false,
                "ConnectionClose should be modified by Connection.Clear().");
            Assert.Equal(0, headers.Connection.Count);
            IEnumerable<string> dummyArray;
            Assert.False(headers.TryGetValues("Connection", out dummyArray),
                "Connection header count after Connection.Clear().");

            // Remove 'close' value from store. Since there are no other 'Connection' values, remove whole header.
            headers.ConnectionClose = false;
            Assert.True(headers.ConnectionClose == false, "ConnectionClose == false");
            Assert.Equal(0, headers.Connection.Count);
            Assert.False(headers.Contains("Connection"));

            headers.ConnectionClose = null;
            Assert.Null(headers.ConnectionClose);
        }

        [Fact]
        public void Connection_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Connection", "custom1, close, custom2, custom3");

            // Connection collection has 3 values plus 'close'
            Assert.Equal(4, headers.Connection.Count);
            Assert.Equal(4, headers.GetValues("Connection").Count());
            Assert.True(headers.ConnectionClose == true);

            Assert.Equal("custom1", headers.Connection.ElementAt(0));
            Assert.Equal("close", headers.Connection.ElementAt(1));
            Assert.Equal("custom2", headers.Connection.ElementAt(2));
            Assert.Equal("custom3", headers.Connection.ElementAt(3));

            headers.Connection.Clear();
            Assert.Null(headers.ConnectionClose);
            Assert.Equal(0, headers.Connection.Count);
            IEnumerable<string> dummyArray;
            Assert.False(headers.TryGetValues("Connection", out dummyArray),
                "Connection header count after Connection.Clear().");
        }

        [Fact]
        public void Connection_AddInvalidValue_Throw()
        {
            Assert.Throws<FormatException>(() => { headers.Connection.Add("this is invalid"); });
        }

        [Fact]
        public void TransferEncoding_AddChunked_Success()
        {
            // use non-default casing to make sure we do case-insensitive comparison.
            headers.TransferEncoding.Add(new TransferCodingHeaderValue("CHUNKED"));
            Assert.True(headers.TransferEncodingChunked == true);
            Assert.Equal(1, headers.TransferEncoding.Count);
        }

        [Fact]
        public void TransferEncoding_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.TransferEncoding.Count);
            Assert.Null(headers.TransferEncodingChunked);

            headers.TransferEncoding.Add(new TransferCodingHeaderValue("custom1"));
            headers.TransferEncoding.Add(new TransferCodingHeaderValue("custom2"));
            headers.TransferEncodingChunked = true;

            // Connection collection has 2 values plus 'chunked'
            Assert.Equal(3, headers.TransferEncoding.Count);
            Assert.Equal(3, headers.GetValues("Transfer-Encoding").Count());
            Assert.True(headers.TransferEncodingChunked);
            Assert.Equal(new TransferCodingHeaderValue("custom1"), headers.TransferEncoding.ElementAt(0));
            Assert.Equal(new TransferCodingHeaderValue("custom2"), headers.TransferEncoding.ElementAt(1));

            // Remove 'chunked' value from store. But leave other 'Transfer-Encoding' values. Note that according to
            // the RFC this is not valid, since 'chunked' must always be present. However this check is done
            // in the transport handler since the user can add invalid header values anyways.
            headers.TransferEncodingChunked = false;
            Assert.True(headers.TransferEncodingChunked == false, "TransferEncodingChunked == false");
            Assert.Equal(2, headers.TransferEncoding.Count);
            Assert.Equal(new TransferCodingHeaderValue("custom1"), headers.TransferEncoding.ElementAt(0));
            Assert.Equal(new TransferCodingHeaderValue("custom2"), headers.TransferEncoding.ElementAt(1));

            headers.TransferEncodingChunked = true;
            headers.TransferEncoding.Clear();
            Assert.True(headers.TransferEncodingChunked == false,
                "TransferEncodingChunked should be modified by TransferEncoding.Clear().");
            Assert.Equal(0, headers.TransferEncoding.Count);
            Assert.False(headers.Contains("Transfer-Encoding"));

            // Remove 'chunked' value from store. Since there are no other 'Transfer-Encoding' values, remove whole
            // header.
            headers.TransferEncodingChunked = false;
            Assert.True(headers.TransferEncodingChunked == false, "TransferEncodingChunked == false");
            Assert.Equal(0, headers.TransferEncoding.Count);
            Assert.False(headers.Contains("Transfer-Encoding"));

            headers.TransferEncodingChunked = null;
            Assert.Null(headers.TransferEncodingChunked);
        }

        [Fact]
        public void TransferEncoding_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Transfer-Encoding", " , custom1, , custom2, custom3, chunked    ,");

            // Connection collection has 3 values plus 'chunked'
            Assert.Equal(4, headers.TransferEncoding.Count);
            Assert.Equal(4, headers.GetValues("Transfer-Encoding").Count());
            Assert.True(headers.TransferEncodingChunked == true, "TransferEncodingChunked expected to be true.");

            Assert.Equal(new TransferCodingHeaderValue("custom1"), headers.TransferEncoding.ElementAt(0));
            Assert.Equal(new TransferCodingHeaderValue("custom2"), headers.TransferEncoding.ElementAt(1));
            Assert.Equal(new TransferCodingHeaderValue("custom3"), headers.TransferEncoding.ElementAt(2));

            headers.TransferEncoding.Clear();
            Assert.Null(headers.TransferEncodingChunked);
            Assert.Equal(0, headers.TransferEncoding.Count);
            Assert.False(headers.Contains("Transfer-Encoding"),
                "Transfer-Encoding header after TransferEncoding.Clear().");
        }

        [Fact]
        public void TransferEncoding_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Transfer-Encoding", "custom\u4F1A");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.TransferEncoding.Descriptor));
            Assert.Equal(1, headers.GetValues("Transfer-Encoding").Count());
            Assert.Equal("custom\u4F1A", headers.GetValues("Transfer-Encoding").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Transfer-Encoding", "custom1 custom2");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.TransferEncoding.Descriptor));
            Assert.Equal(1, headers.GetValues("Transfer-Encoding").Count());
            Assert.Equal("custom1 custom2", headers.GetValues("Transfer-Encoding").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Transfer-Encoding", "");
            Assert.False(headers.Contains("Transfer-Encoding"), "'Transfer-Encoding' header should not be added if it just has empty values.");
        }

        [Fact]
        public void Upgrade_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.Upgrade.Count);

            headers.Upgrade.Add(new ProductHeaderValue("custom1"));
            headers.Upgrade.Add(new ProductHeaderValue("custom2", "1.1"));

            Assert.Equal(2, headers.Upgrade.Count);
            Assert.Equal(2, headers.GetValues("Upgrade").Count());
            Assert.Equal(new ProductHeaderValue("custom1"), headers.Upgrade.ElementAt(0));
            Assert.Equal(new ProductHeaderValue("custom2", "1.1"), headers.Upgrade.ElementAt(1));

            headers.Upgrade.Clear();
            Assert.Equal(0, headers.Upgrade.Count);
            Assert.False(headers.Contains("Upgrade"), "Upgrade header should be removed after calling Clear().");
        }

        [Fact]
        public void Upgrade_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Upgrade", " , custom1 / 1.0, , custom2, custom3/2.0,");

            Assert.Equal(3, headers.Upgrade.Count);
            Assert.Equal(3, headers.GetValues("Upgrade").Count());

            Assert.Equal(new ProductHeaderValue("custom1", "1.0"), headers.Upgrade.ElementAt(0));
            Assert.Equal(new ProductHeaderValue("custom2"), headers.Upgrade.ElementAt(1));
            Assert.Equal(new ProductHeaderValue("custom3", "2.0"), headers.Upgrade.ElementAt(2));

            headers.Upgrade.Clear();
            Assert.Equal(0, headers.Upgrade.Count);
            Assert.False(headers.Contains("Upgrade"), "Upgrade header should be removed after calling Clear().");
        }

        [Fact]
        public void Upgrade_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Upgrade", "custom\u4F1A");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.Upgrade.Descriptor));
            Assert.Equal(1, headers.GetValues("Upgrade").Count());
            Assert.Equal("custom\u4F1A", headers.GetValues("Upgrade").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Upgrade", "custom1 custom2");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.Upgrade.Descriptor));
            Assert.Equal(1, headers.GetValues("Upgrade").Count());
            Assert.Equal("custom1 custom2", headers.GetValues("Upgrade").First());
        }

        [Fact]
        public void Date_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.Date);

            DateTimeOffset expected = DateTimeOffset.Now;
            headers.Date = expected;
            Assert.Equal(expected, headers.Date);

            headers.Date = null;
            Assert.Null(headers.Date);
            Assert.False(headers.Contains("Date"),
                "Header store should not contain a header 'Date' after setting it to null.");

            // Make sure the header gets serialized correctly
            headers.Date = (new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero));
            Assert.Equal("Sun, 06 Nov 1994 08:49:37 GMT", headers.GetValues("Date").First());
        }

        [Fact]
        public void Date_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Date", "  Sun, 06 Nov 1994 08:49:37 GMT  ");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), headers.Date);

            headers.Clear();
            headers.TryAddWithoutValidation("Date", "Sun, 06 Nov 1994 08:49:37 GMT");
            Assert.Equal(new DateTimeOffset(1994, 11, 6, 8, 49, 37, TimeSpan.Zero), headers.Date);
        }

        [Fact]
        public void Date_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Date", " Sun, 06 Nov 1994 08:49:37 GMT ,");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.Date.Descriptor));
            Assert.Equal(1, headers.GetValues("Date").Count());
            Assert.Equal(" Sun, 06 Nov 1994 08:49:37 GMT ,", headers.GetValues("Date").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Date", " Sun, 06 Nov ");
            Assert.Null(headers.GetSingleParsedValue(KnownHeaders.Date.Descriptor));
            Assert.Equal(1, headers.GetValues("Date").Count());
            Assert.Equal(" Sun, 06 Nov ", headers.GetValues("Date").First());
        }

        [Fact]
        public void Via_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.Via.Count);

            headers.Via.Add(new ViaHeaderValue("x11", "host"));
            headers.Via.Add(new ViaHeaderValue("1.1", "example.com:8080", "HTTP", "(comment)"));

            Assert.Equal(2, headers.Via.Count);
            Assert.Equal(new ViaHeaderValue("x11", "host"), headers.Via.ElementAt(0));
            Assert.Equal(new ViaHeaderValue("1.1", "example.com:8080", "HTTP", "(comment)"),
                headers.Via.ElementAt(1));

            headers.Via.Clear();
            Assert.Equal(0, headers.Via.Count);
        }

        [Fact]
        public void Via_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Via", ", 1.1 host, WS/1.0 [::1],X/11 192.168.0.1 (c(comment)) ");

            Assert.Equal(new ViaHeaderValue("1.1", "host"), headers.Via.ElementAt(0));
            Assert.Equal(new ViaHeaderValue("1.0", "[::1]", "WS"), headers.Via.ElementAt(1));
            Assert.Equal(new ViaHeaderValue("11", "192.168.0.1", "X", "(c(comment))"), headers.Via.ElementAt(2));

            headers.Via.Clear();
            headers.TryAddWithoutValidation("Via", "");
            Assert.Equal(0, headers.Via.Count);
            Assert.False(headers.Contains("Via"));
        }

        [Fact]
        public void Via_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Via", "1.1 host1 1.1 host2"); // no separator
            Assert.Equal(0, headers.Via.Count);
            Assert.Equal(1, headers.GetValues("Via").Count());
            Assert.Equal("1.1 host1 1.1 host2", headers.GetValues("Via").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Via", "X/11 host/1");
            Assert.Equal(0, headers.Via.Count);
            Assert.Equal(1, headers.GetValues("Via").Count());
            Assert.Equal("X/11 host/1", headers.GetValues("Via").First());
        }

        [Fact]
        public void Warning_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.Warning.Count);

            headers.Warning.Add(new WarningHeaderValue(199, "microsoft.com", "\"Miscellaneous warning\""));
            headers.Warning.Add(new WarningHeaderValue(113, "example.com", "\"Heuristic expiration\""));

            Assert.Equal(2, headers.Warning.Count);
            Assert.Equal(new WarningHeaderValue(199, "microsoft.com", "\"Miscellaneous warning\""),
                headers.Warning.ElementAt(0));
            Assert.Equal(new WarningHeaderValue(113, "example.com", "\"Heuristic expiration\""),
                headers.Warning.ElementAt(1));

            headers.Warning.Clear();
            Assert.Equal(0, headers.Warning.Count);
        }

        [Fact]
        public void Warning_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Warning",
                "112 example.com \"Disconnected operation\", 111 example.org \"Revalidation failed\"");

            Assert.Equal(new WarningHeaderValue(112, "example.com", "\"Disconnected operation\""),
                headers.Warning.ElementAt(0));
            Assert.Equal(new WarningHeaderValue(111, "example.org", "\"Revalidation failed\""),
                headers.Warning.ElementAt(1));

            headers.Warning.Clear();
            headers.TryAddWithoutValidation("Warning", "");
            Assert.Equal(0, headers.Warning.Count);
            Assert.False(headers.Contains("Warning"));
        }

        [Fact]
        public void Warning_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Warning", "123 host1 \"\" 456 host2 \"\""); // no separator
            Assert.Equal(0, headers.Warning.Count);
            Assert.Equal(1, headers.GetValues("Warning").Count());
            Assert.Equal("123 host1 \"\" 456 host2 \"\"", headers.GetValues("Warning").First());

            headers.Clear();
            headers.TryAddWithoutValidation("Warning", "123 host1\"text\"");
            Assert.Equal(0, headers.Warning.Count);
            Assert.Equal(1, headers.GetValues("Warning").Count());
            Assert.Equal("123 host1\"text\"", headers.GetValues("Warning").First());
        }

        [Fact]
        public void CacheControl_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Null(headers.CacheControl);

            CacheControlHeaderValue value = new CacheControlHeaderValue();
            value.NoCache = true;
            value.NoCacheHeaders.Add("token1");
            value.NoCacheHeaders.Add("token2");
            value.MustRevalidate = true;
            value.SharedMaxAge = new TimeSpan(1, 2, 3);
            headers.CacheControl = value;
            Assert.Equal(value, headers.CacheControl);

            headers.CacheControl = null;
            Assert.Null(headers.CacheControl);
            Assert.False(headers.Contains("Cache-Control"),
                "Header store should not contain a header 'Cache-Control' after setting it to null.");
        }

        [Fact]
        public void CacheControl_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Cache-Control", "no-cache=\"token1, token2\", must-revalidate, max-age=3");
            headers.Add("Cache-Control", "");
            headers.Add("Cache-Control", "public, s-maxage=15");
            headers.TryAddWithoutValidation("Cache-Control", "");

            CacheControlHeaderValue value = new CacheControlHeaderValue();
            value.NoCache = true;
            value.NoCacheHeaders.Add("token1");
            value.NoCacheHeaders.Add("token2");
            value.MustRevalidate = true;
            value.MaxAge = new TimeSpan(0, 0, 3);
            value.Public = true;
            value.SharedMaxAge = new TimeSpan(0, 0, 15);
            Assert.Equal(value, headers.CacheControl);
        }

        [Fact]
        public void CacheControl_ValidAndInvalidValues_ReturnValidValue()
        {
            headers.TryAddWithoutValidation("Cache-Control", "<invalid>");
            headers.TryAddWithoutValidation("Cache-Control", "no-cache=\"token1\", must-revalidate, max-age=3");
            headers.TryAddWithoutValidation("Cache-Control", "public, s-maxage=15");
            Assert.True(headers.CacheControl.NoCache);
            Assert.True(headers.NonValidated["Cache-Control"].Count == 2);
            Assert.Equal("<invalid>", headers.NonValidated["Cache-Control"].ElementAt(0));
            Assert.Equal("public, must-revalidate, no-cache=\"token1\", max-age=3, s-maxage=15", headers.NonValidated["Cache-Control"].ElementAt(1));
        }

        [Fact]
        public void Trailer_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.Trailer.Count);

            headers.Trailer.Add("custom1");
            headers.Trailer.Add("custom2");

            Assert.Equal(2, headers.Trailer.Count);
            Assert.Equal(2, headers.GetValues("Trailer").Count());

            Assert.Equal("custom1", headers.Trailer.ElementAt(0));
            Assert.Equal("custom2", headers.Trailer.ElementAt(1));

            headers.Trailer.Clear();
            Assert.Equal(0, headers.Trailer.Count);
            Assert.False(headers.Contains("Trailer"),
                "There should be no Trailer header after calling Clear().");
        }

        [Fact]
        public void Trailer_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Trailer", ",custom1, custom2, custom3,");

            Assert.Equal(3, headers.Trailer.Count);
            Assert.Equal(3, headers.GetValues("Trailer").Count());

            Assert.Equal("custom1", headers.Trailer.ElementAt(0));
            Assert.Equal("custom2", headers.Trailer.ElementAt(1));
            Assert.Equal("custom3", headers.Trailer.ElementAt(2));

            headers.Trailer.Clear();
            Assert.Equal(0, headers.Trailer.Count);
            Assert.False(headers.Contains("Trailer"),
                "There should be no Trailer header after calling Clear().");
        }

        [Fact]
        public void Trailer_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Trailer", "custom1 custom2"); // no separator

            Assert.Equal(0, headers.Trailer.Count);
            Assert.Equal(1, headers.GetValues("Trailer").Count());
            Assert.Equal("custom1 custom2", headers.GetValues("Trailer").First());
        }

        [Fact]
        public void Pragma_ReadAndWriteProperty_ValueMatchesPriorSetValue()
        {
            Assert.Equal(0, headers.Pragma.Count);

            headers.Pragma.Add(new NameValueHeaderValue("custom1", "value1"));
            headers.Pragma.Add(new NameValueHeaderValue("custom2"));

            Assert.Equal(2, headers.Pragma.Count);
            Assert.Equal(2, headers.GetValues("Pragma").Count());

            Assert.Equal(new NameValueHeaderValue("custom1", "value1"), headers.Pragma.ElementAt(0));
            Assert.Equal(new NameValueHeaderValue("custom2"), headers.Pragma.ElementAt(1));

            headers.Pragma.Clear();
            Assert.Equal(0, headers.Pragma.Count);
            Assert.False(headers.Contains("Pragma"),
                "There should be no Pragma header after calling Clear().");
        }

        [Fact]
        public void Pragma_UseAddMethod_AddedValueCanBeRetrievedUsingProperty()
        {
            headers.TryAddWithoutValidation("Pragma", ",custom1=value1, custom2, custom3=value3,");

            Assert.Equal(3, headers.Pragma.Count);
            Assert.Equal(3, headers.GetValues("Pragma").Count());

            Assert.Equal(new NameValueHeaderValue("custom1", "value1"), headers.Pragma.ElementAt(0));
            Assert.Equal(new NameValueHeaderValue("custom2"), headers.Pragma.ElementAt(1));
            Assert.Equal(new NameValueHeaderValue("custom3", "value3"), headers.Pragma.ElementAt(2));

            headers.Pragma.Clear();
            Assert.Equal(0, headers.Pragma.Count);
            Assert.False(headers.Contains("Pragma"),
                "There should be no Pragma header after calling Clear().");
        }

        [Fact]
        public void Pragma_UseAddMethodWithInvalidValue_InvalidValueRecognized()
        {
            headers.TryAddWithoutValidation("Pragma", "custom1, custom2=");

            Assert.Equal(0, headers.Pragma.Count());
            Assert.Equal(1, headers.GetValues("Pragma").Count());
            Assert.Equal("custom1, custom2=", headers.GetValues("Pragma").First());
        }

        #endregion

        [Fact]
        public void ToString_SeveralRequestHeaders_Success()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            string expected = string.Empty;

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/xml"));
            expected += HttpKnownHeaderNames.Accept + ": application/xml, */xml" + Environment.NewLine;

            request.Headers.Authorization = new AuthenticationHeaderValue("Basic");
            expected += HttpKnownHeaderNames.Authorization + ": Basic" + Environment.NewLine;

            request.Headers.ExpectContinue = true;
            expected += HttpKnownHeaderNames.Expect + ": 100-continue" + Environment.NewLine;

            request.Headers.TransferEncodingChunked = true;
            expected += HttpKnownHeaderNames.TransferEncoding + ": chunked" + Environment.NewLine;

            Assert.Equal(expected, request.Headers.ToString());
        }

        [Fact]
        public void CustomHeaders_ResponseHeadersAsCustomHeaders_Success()
        {
            // Header names reserved for response headers are permitted as custom request headers.
            headers.Add("Accept-Ranges", "v");
            headers.TryAddWithoutValidation("age", "v");
            headers.Add("ETag", "v");
            headers.Add("Location", "v");
            headers.Add("Proxy-Authenticate", "v");
            headers.Add("Retry-After", "v");
            headers.Add("Server", "v");
            headers.Add("Vary", "v");
            headers.Add("WWW-Authenticate", "v");
        }

        [Fact]
        public void InvalidHeaders_AddContentHeaders_Throw()
        {
            // Try adding content headers. Use different casing to make sure case-insensitive comparison
            // is used.

            Assert.Throws<InvalidOperationException>(() => { headers.Add("Allow", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-Encoding", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-Language", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("content-length", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-Location", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-MD5", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Content-Range", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("CONTENT-TYPE", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Expires", "v"); });
            Assert.Throws<InvalidOperationException>(() => { headers.Add("Last-Modified", "v"); });
        }
    }
}
