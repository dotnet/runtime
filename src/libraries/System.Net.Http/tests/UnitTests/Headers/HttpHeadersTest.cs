// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using System.Tests;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpHeadersTest
    {
        // Note: These are not real known headers, so they won't be returned if we call HeaderDescriptor.Get().
        private static readonly HeaderDescriptor known1Header = (new KnownHeader("known1", HttpHeaderType.General, new MockHeaderParser())).Descriptor;
        private static readonly HeaderDescriptor known2Header = (new KnownHeader("known2", HttpHeaderType.General, new MockHeaderParser())).Descriptor;
        private static readonly HeaderDescriptor known3Header = (new KnownHeader("known3", HttpHeaderType.General, new MockHeaderParser())).Descriptor;
        private static readonly HeaderDescriptor known4Header = (new KnownHeader("known3", HttpHeaderType.General, new CustomTypeHeaderParser())).Descriptor;

        private static readonly HeaderDescriptor noComparerHeader = (new KnownHeader("noComparerHeader", HttpHeaderType.General, new NoComparerHeaderParser())).Descriptor;
        private static readonly HeaderDescriptor customTypeHeader = (new KnownHeader("customTypeHeader", HttpHeaderType.General, new CustomTypeHeaderParser())).Descriptor;

        private static readonly HeaderDescriptor customHeader;

        static HttpHeadersTest()
        {
            HeaderDescriptor.TryGet("custom", out customHeader);
        }

        private const string customHeaderName = "custom-header";
        private const string rawPrefix = "raw";
        private const string parsedPrefix = "parsed";
        private const string invalidHeaderValue = "invalid";

        [Fact]
        public void TryAddWithoutValidation_ValidAndInvalidValues_InsertionOrderIsPreserved()
        {
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "http://microsoft.com");
            request.Headers.TryAddWithoutValidation("Accept", "text/bar");
            request.Headers.TryAddWithoutValidation("Accept", "invalid");
            request.Headers.TryAddWithoutValidation("Accept", "text/baz");

            // Force parsing
            _ = request.Headers.Accept.Count;

            Assert.Equal(1, request.Headers.NonValidated.Count);
            HeaderStringValues accept = request.Headers.NonValidated["Accept"];
            Assert.Equal(new[] { "text/bar", "invalid", "text/baz" }, accept);
        }

        [Fact]
        public void MixedAddAndTryAddWithoutValidation_ParsedAndRawValues_InsertionOrderIsPreserved()
        {
            HttpHeaders headers = new HttpRequestMessage().Headers;

            headers.Add("Accept", "text/foo");
            headers.TryAddWithoutValidation("Accept", "text/bar");
            headers.TryAddWithoutValidation("Accept", "invalid");

            var expectedValues = new string[] { "text/foo", "text/bar", "invalid" };

            Assert.Equal(expectedValues, headers.NonValidated["Accept"]);

            Assert.True(headers.TryGetValues("Accept", out IEnumerable<string>? values));
            Assert.Equal(expectedValues, values);

            Assert.Equal(expectedValues, headers.NonValidated["Accept"]);
        }

        [Fact]
        public void TryAddWithoutValidation_AddInvalidViaHeaderValue_ValuePassed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation("Via", "1.1 foo.bar, foo");

            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal("1.1 foo.bar, foo", headers.First().Value.ElementAt(0));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryAddWithoutValidation_UseEmptyHeaderName_False(string headerName)
        {
            MockHeaders headers = new MockHeaders();
            Assert.False(headers.TryAddWithoutValidation(headerName, "value"));
        }

        [Theory]
        [MemberData(nameof(GetInvalidHeaderNames))]
        public void TryAddWithoutValidation_UseInvalidHeaderName_False(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            Assert.False(headers.TryAddWithoutValidation(headerName, "value"));
        }

        [Fact]
        public void TryAddWithoutValidation_AddSingleValue_ValueParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix);

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix, headers.First().Value.First());

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddTwoSingleValues_BothValuesParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddTwoValidValuesAsOneString_BothValuesParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1," + rawPrefix + "2");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));

            // The parser gets called for each value in the raw string. I.e. if we have 1 raw string containing two
            // values, the parser gets called twice.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddTwoValuesOneValidOneInvalidAsOneString_RawStringAddedAsInvalid()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1," + invalidHeaderValue);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());

            // We expect the value to be returned without change since it couldn't be parsed in its entirety.
            Assert.Equal(rawPrefix + "1," + invalidHeaderValue, headers.First().Value.ElementAt(0));

            // The parser gets called twice, but the second time it returns false, because it tries to parse
            // 'invalidHeaderValue'.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddTwoValueStringAndThirdValue_AllValuesParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1," + rawPrefix + "2");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "3");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(3, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(parsedPrefix + "3", headers.First().Value.ElementAt(2));

            Assert.Equal(3, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddInvalidAndValidValueString_BothValuesParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix);
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(parsedPrefix, headers.First().Value.ElementAt(0));
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(1));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddEmptyValueString_HeaderWithNoValueAfterParsing()
        {
            MockHeaders headers = new MockHeaders();

            // The parser returns 'true' to indicate that it could parse the value (empty values allowed) and an
            // value of 'null'.
            headers.TryAddWithoutValidation(headers.Descriptor, string.Empty);
            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
            Assert.Equal(1, headers.Count());

            headers.Clear();
            headers.TryAddWithoutValidation("custom", (string)null);
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(string.Empty, headers.GetValues("custom").First());
        }

        [Fact]
        public void TryAddWithoutValidation_AddValidAndInvalidValueString_BothValuesParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue);
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix, headers.First().Value.ElementAt(1));
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            string expected = headers.Descriptor.Name + ": " + invalidHeaderValue + ", " + parsedPrefix + Environment.NewLine;
            Assert.Equal(expected, headers.ToString());
        }

        [Fact]
        public void TryAddWithoutValidation_AddNullValueForKnownHeader_ParserDoesNotRejectsNullEmptyStringAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, (string)null);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            // MockParser is called with an empty string and decides that it is OK to have empty values.
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddNullValueForUnknownHeader_EmptyStringAddedAsValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(customHeaderName, (string)null);

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());

            // 'null' values are internally stored as string.Empty. Since we added a custom header, there is no
            // parser and the empty string is just added to the list of 'parsed values'.
            Assert.Equal(string.Empty, headers.First().Value.First());

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddValueForUnknownHeader_ValueAddedToStore()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(customHeaderName, "custom value");

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());

            Assert.Equal("custom value", headers.First().Value.First());

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddNullAndEmptyValuesToKnownHeader_HeaderNotRemovedFromCollection()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, (string)null);
            headers.TryAddWithoutValidation(headers.Descriptor, string.Empty);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
            Assert.Equal(1, headers.Count());

            // TryAddWithoutValidation() adds 'null' as string.empty to distinguish between an empty raw value and no raw
            // value. When the parser is called later, the parser can decide whether empty strings are valid or not.
            // In our case the MockParser returns 'success' with a parsed value of 'null' indicating that it is OK to
            // have empty values.
            Assert.Equal(2, headers.Parser.EmptyValueCount);
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddNullAndEmptyValuesToUnknownHeader_TwoEmptyStringsAddedAsValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(customHeaderName, (string)null);
            headers.TryAddWithoutValidation(customHeaderName, string.Empty);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            // TryAddWithoutValidation() adds 'null' as string.empty to distinguish between an empty raw value and no raw
            // value. For custom headers we just add what the user gives us. I.e. the result is a header with two empty
            // values.
            Assert.Equal(string.Empty, headers.First().Value.ElementAt(0));
            Assert.Equal(string.Empty, headers.First().Value.ElementAt(1));
        }

        [Fact]
        public void TryAddWithoutValidation_AddMultipleValueToSingleValueHeaders_FirstHeaderAddedOthersAreInvalid()
        {
            MockHeaderParser parser = new MockHeaderParser(false); // doesn't support multiple values.
            MockHeaders headers = new MockHeaders(parser);
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            // Note that the first value was parsed and added to the 'parsed values' list. The second value however
            // was added to the 'invalid values' list since the header doesn't support multiple values.
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(rawPrefix + "2", headers.First().Value.ElementAt(1));

            // The parser is only called once for the first value. HttpHeaders doesn't invoke the parser for
            // additional values if the parser only supports one value.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddMultipleValueStringToSingleValueHeaders_MultipleValueStringAddedAsInvalid()
        {
            MockHeaderParser parser = new MockHeaderParser(false); // doesn't support multiple values.
            MockHeaders headers = new MockHeaders(parser);
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1," + rawPrefix + "2");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            // Since parsing the header value fails because it is composed of 2 values, the original string is added
            // to the list of 'invalid values'. Therefore we only have 1 header value (the original string).
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(rawPrefix + "1," + rawPrefix + "2", headers.First().Value.First());

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Theory]
        [MemberData(nameof(HeaderValuesWithNewLines))]
        public void TryAddWithoutValidation_AddValueContainingNewLine_InvalidValueSaved(string headerValue)
        {
            var headers = new HttpRequestHeaders();

            // This value is considered invalid, however, since TryAddWithoutValidation() only causes the
            // header value to be analyzed when it gets actually accessed, no exception is thrown.
            headers.TryAddWithoutValidation("foo", headerValue);

            Assert.True(headers.Contains("foo"));
            Assert.Equal(1, headers.NonValidated.Count);
            Assert.Equal(headerValue, headers.NonValidated["foo"].ToString());

            Assert.Equal(1, headers.Count());
            Assert.True(headers.Contains("foo"));

            Assert.Equal(1, headers.NonValidated.Count);

            headers.Clear();
            headers.TryAddWithoutValidation("foo", new[] { "valid", headerValue });

            Assert.Equal(1, headers.NonValidated.Count);
            Assert.Equal(new[] { "valid", headerValue }, headers.NonValidated["foo"]);

            Assert.Equal(1, headers.Count());
            Assert.Equal(new[] { "valid", headerValue }, headers.First().Value);

            Assert.Equal(1, headers.NonValidated.Count);
            Assert.Equal(new[] { "valid", headerValue }, headers.NonValidated["foo"]);
        }

        [Fact]
        public void TryAddWithoutValidation_MultipleAddInvalidValuesToNonExistingHeader_AddHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, new string[] { invalidHeaderValue });

            // Make sure the header did not get added since we just tried to add an invalid value.
            Assert.True(headers.Contains(headers.Descriptor));
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void TryAddWithoutValidation_MultipleAddValidValueThenAddInvalidValuesToExistingHeader_AddValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, new string[] { rawPrefix + "2", invalidHeaderValue });

            Assert.True(headers.Contains(headers.Descriptor));
            Assert.Equal(3, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(2));
        }

        [Fact]
        public void TryAddWithoutValidation_MultipleAddValidValueThenAddInvalidValuesToNonExistingHeader_AddHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, new string[] { rawPrefix + "1", invalidHeaderValue });

            Assert.True(headers.Contains(headers.Descriptor));
            Assert.Equal(2, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(1));
        }

        [Fact]
        public void TryAddWithoutValidation_MultipleAddNullValueCollection_Throws()
        {
            MockHeaders headers = new MockHeaders();
            string[] values = null;

            Assert.Throws<ArgumentNullException>(() => { headers.TryAddWithoutValidation(headers.Descriptor, values); });
        }

        [Theory]
        [InlineData(null)]
        public void Add_SingleUseNullHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            AssertExtensions.Throws<ArgumentNullException>("name", () => { headers.Add(headerName, "value"); });
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t\r\n ")]
        public void Add_SingleUseWhiteSpaceHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            AssertExtensions.Throws<ArgumentException>("name", () => { headers.Add(headerName, "value"); });
        }

        [Theory]
        [MemberData(nameof(GetInvalidHeaderNames))]
        public void Add_SingleUseInvalidHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            Assert.Throws<FormatException>(() => { headers.Add(headerName, "value"); });
        }

        [Fact]
        public void Add_SingleUseStoreWithNoParserStore_AllHeadersConsideredCustom()
        {
            CustomTypeHeaders headers = new CustomTypeHeaders();
            headers.Add("custom", "value");

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal("value", headers.First().Value.First());
        }

        [Fact]
        public void Add_SingleAddValidValue_ValueParsedCorrectly()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix);

            // Add() should trigger parsing.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());

            Assert.Equal(parsedPrefix, headers.First().Value.ElementAt(0));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleAddEmptyValueMultipleTimes_EmptyHeaderAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, (string)null);
            headers.Add(headers.Descriptor, string.Empty);
            headers.Add(headers.Descriptor, string.Empty);

            // Add() should trigger parsing.
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);

            Assert.Equal(0, headers.Count());
        }

        [Fact]
        public void Add_SingleAddInvalidValueToNonExistingHeader_ThrowAndDontAddHeader()
        {
            // Since Add() immediately parses the value, it will throw an exception if the value is invalid.
            MockHeaders headers = new MockHeaders();
            Assert.Throws<FormatException>(() => { headers.Add(headers.Descriptor, invalidHeaderValue); });

            // Make sure the header did not get added to the store.
            Assert.False(headers.Contains(headers.Descriptor),
                "No header expected to be added since header value was invalid.");
        }

        [Fact]
        public void Add_SingleAddValidValueThenAddInvalidValue_ThrowAndHeaderContainsValidValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix);

            Assert.Throws<FormatException>(() => { headers.Add(headers.Descriptor, invalidHeaderValue); });

            // Make sure the header did not get removed due to the failed add.
            Assert.True(headers.Contains(headers.Descriptor), "Header was removed even if there is a valid header value.");
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix, headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void Add_MultipleAddInvalidValuesToNonExistingHeader_ThrowAndDontAddHeader()
        {
            MockHeaders headers = new MockHeaders();

            Assert.Throws<FormatException>(() => { headers.Add(headers.Descriptor, new string[] { invalidHeaderValue }); });

            // Make sure the header did not get added since we just tried to add an invalid value.
            Assert.False(headers.Contains(headers.Descriptor), "Header was added even if we just added an invalid value.");
        }

        [Fact]
        public void Add_MultipleAddValidValueThenAddInvalidValuesToExistingHeader_ThrowAndDontAddHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");

            Assert.Throws<FormatException>(() => { headers.Add(headers.Descriptor, new string[] { rawPrefix + "2", invalidHeaderValue }); });

            // Make sure the header did not get removed due to the failed add. Note that the first value in the array
            // is valid, so it gets added. I.e. we have 2 values.
            Assert.True(headers.Contains(headers.Descriptor), "Header was removed even if there is a valid header value.");
            Assert.Equal(2, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
        }

        [Fact]
        public void Add_MultipleAddValidValueThenAddInvalidValuesToNonExistingHeader_ThrowAndDontAddHeader()
        {
            MockHeaders headers = new MockHeaders();

            Assert.Throws<FormatException>(() => { headers.Add(headers.Descriptor, new string[] { rawPrefix + "1", invalidHeaderValue }); });

            // Make sure the header got added due to the valid add. Note that the first value in the array
            // is valid, so it gets added.
            Assert.True(headers.Contains(headers.Descriptor), "Header was not added even though we added 1 valid value.");
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void Add_SingleAddThreeValidValues_ValuesParsedCorrectly()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.Add(headers.Descriptor, rawPrefix + "2");
            headers.Add(headers.Descriptor, rawPrefix + "3");

            // Add() should trigger parsing.
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(3, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(parsedPrefix + "3", headers.First().Value.ElementAt(2));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleAddTwoValidValuesToHeaderWithSingleValue_Throw()
        {
            MockHeaderParser parser = new MockHeaderParser(false); // doesn't support multiple values.
            MockHeaders headers = new MockHeaders(parser);

            headers.Add(headers.Descriptor, rawPrefix + "1");
            // Can only add headers once.
            Assert.Throws<FormatException>(() => { headers.Add(headers.Descriptor, rawPrefix + "2"); });

            // Verify that the first header value is still there.
            Assert.Equal(1, headers.First().Value.Count());
        }

        [Fact]
        public void Add_SingleFirstTryAddWithoutValidationForValidValueThenAdd_TwoParsedValuesAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1");
            headers.Add(headers.Descriptor, rawPrefix + "2");

            // Add() should trigger parsing. TryAddWithoutValidation() doesn't trigger parsing, but Add() triggers
            // parsing of raw header values (TryParseValue() is called)
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleFirstTryAddWithoutValidationForInvalidValueThenAdd_TwoParsedValuesAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue);
            headers.Add(headers.Descriptor, rawPrefix + "1");

            // Add() should trigger parsing. TryAddWithoutValidation() doesn't trigger parsing, but Add() triggers
            // parsing of raw header values (TryParseValue() is called)
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(1));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleFirstTryAddWithoutValidationForEmptyValueThenAdd_TwoParsedValueAddedEmptyNotIgnored()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, string.Empty);
            headers.Add(headers.Descriptor, rawPrefix + "1");

            // Add() should trigger parsing. TryAddWithoutValidation() doesn't trigger parsing, but Add() triggers
            // parsing of raw header values (TryParseValue() is called)
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(1));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleFirstAddThenTryAddWithoutValidation_TwoParsedValuesAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            // Add() should trigger parsing. Since TryAddWithoutValidation() is called afterwards the second value is
            // not parsed yet.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleAddThenTryAddWithoutValidationThenAdd_ThreeParsedValuesAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");
            headers.Add(headers.Descriptor, rawPrefix + "3");

            // The second Add() triggers also parsing of the value added by TryAddWithoutValidation()
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(3, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(parsedPrefix + "3", headers.First().Value.ElementAt(2));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleFirstTryAddWithoutValidationThenAddToSingleValueHeader_AddThrows()
        {
            MockHeaderParser parser = new MockHeaderParser(false); // doesn't support multiple values.
            MockHeaders headers = new MockHeaders(parser);

            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1");
            Assert.Throws<FormatException>(() => {headers.Add(headers.Descriptor, rawPrefix + "2"); });
        }

        [Fact]
        public void Add_SingleFirstAddThenTryAddWithoutValidationToSingleValueHeader_BothParsedAndInvalidValue()
        {
            MockHeaderParser parser = new MockHeaderParser(false); // doesn't support multiple values.
            MockHeaders headers = new MockHeaders(parser);
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Add() succeeds since we don't have a value added yet. TryAddWithoutValidation() also succeeds, however
            // the value is added to the 'invalid values' list when retrieved.
            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(rawPrefix + "2", headers.First().Value.ElementAt(1));

            // Note that TryParseValue() is not called because HttpHeaders sees that there is already a value
            // so it adds the raw value to 'invalid values'.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_MultipleAddThreeValidValuesWithOneCall_ValuesParsedCorrectly()
        {
            MockHeaders headers = new MockHeaders();
            string[] values = new string[] { rawPrefix + "1", rawPrefix + "2", rawPrefix + "3" };
            headers.Add(headers.Descriptor, values);

            // Add() should trigger parsing.
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(3, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(parsedPrefix + "3", headers.First().Value.ElementAt(2));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_MultipleAddThreeValidValuesAsOneString_BothValuesParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1," + rawPrefix + "2," + rawPrefix + "3");

            Assert.Equal(3, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(3, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(parsedPrefix + "3", headers.First().Value.ElementAt(2));
        }

        [Fact]
        public void Add_MultipleAddNullValueCollection_Throw()
        {
            MockHeaders headers = new MockHeaders();
            string[] values = null;

            Assert.Throws<ArgumentNullException>(() => { headers.Add(headers.Descriptor, values); });
        }

        [Fact]
        public void Add_SingleAddCustomHeaderWithNullValue_HeaderIsAddedWithEmptyStringValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(customHeaderName, (string)null);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());

            Assert.Equal(string.Empty, headers.First().Value.ElementAt(0));

            // We're using a custom header. No parsing should be triggered.
            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleAddHeadersWithDifferentCasing_ConsideredTheSameHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom-header", "value1");
            headers.Add("Custom-Header", "value2");
            headers.Add("CUSTOM-HEADER", "value2");

            Assert.Equal(3, headers.GetValues("custom-header").Count());
            Assert.Equal(3, headers.GetValues("Custom-Header").Count());
            Assert.Equal(3, headers.GetValues("CUSTOM-HEADER").Count());
            Assert.Equal(3, headers.GetValues("CuStOm-HeAdEr").Count());
        }

        [Theory]
        [MemberData(nameof(HeaderValuesWithNewLines))]
        public void Add_AddValueContainingNewLine_Rejected(string headerValue)
        {
            var headers = new HttpRequestHeaders();

            Assert.Throws<FormatException>(() => headers.Add("foo", headerValue));
            Assert.Equal(0, headers.Count());
            Assert.Equal(0, headers.NonValidated.Count);

            headers.Clear();
            Assert.Throws<FormatException>(() => headers.Add("foo", new[] { "valid", headerValue }));
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal("valid", headers.First().Value.First());
            Assert.Equal(1, headers.NonValidated.Count);
            Assert.Equal("valid", headers.NonValidated["foo"].ToString());
        }

        [Fact]
        public void RemoveParsedValue_AddValueAndRemoveIt_NoHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");

            // Remove the parsed value (note the original string 'raw1' was "parsed" to 'parsed1')
            Assert.True(headers.RemoveParsedValue(headers.Descriptor, parsedPrefix + "1"));

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(0, headers.Count());

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Remove the value again: It shouldn't be found in the store.
            Assert.False(headers.RemoveParsedValue(headers.Descriptor, parsedPrefix + "1"));
        }

        [Fact]
        public void RemoveParsedValue_AddInvalidValueAndRemoveValidValue_InvalidValueRemains()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue);

            // Remove a valid value which is not in the store.
            Assert.False(headers.RemoveParsedValue(headers.Descriptor, parsedPrefix));

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(1, headers.Count());
            Assert.Equal(invalidHeaderValue, headers.GetValues(headers.Descriptor).First());

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Remove the value again: It shouldn't be found in the store.
            Assert.False(headers.RemoveParsedValue(headers.Descriptor, parsedPrefix + "1"));
        }

        [Fact]
        public void RemoveParsedValue_ParserWithNoEqualityComparer_CaseSensitiveComparison()
        {
            CustomTypeHeaders headers = new CustomTypeHeaders();
            headers.AddParsedValue(noComparerHeader, "lowercasevalue");

            // Since we don't provide a comparer, the default string.Equals() is called which is case-sensitive. So
            // the following call should return false.
            Assert.False(headers.RemoveParsedValue(noComparerHeader, "LOWERCASEVALUE"));

            // Now we try to remove the value using the correct casing. This should work.
            Assert.True(headers.RemoveParsedValue(noComparerHeader, "lowercasevalue"));

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(0, headers.Count());
        }

        [Fact]
        public void RemoveParsedValue_AddTwoValuesAndRemoveThem_NoHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.Add(headers.Descriptor, rawPrefix + "2");

            // Remove the parsed value (note the original string 'raw1' was "parsed" to 'parsed1')
            Assert.True(headers.RemoveParsedValue(headers.Descriptor, parsedPrefix + "1"));
            Assert.True(headers.RemoveParsedValue(headers.Descriptor, parsedPrefix + "2"));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(0, headers.Count());

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void RemoveParsedValue_AddTwoValuesAndRemoveFirstOne_SecondValueRemains()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.Add(headers.Descriptor, rawPrefix + "2");

            // Remove the parsed value (note the original string 'raw1' was "parsed" to 'parsed1')
            Assert.True(headers.RemoveParsedValue(headers.Descriptor, parsedPrefix + "1"));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(0));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void RemoveParsedValue_AddTwoValuesAndRemoveSecondOne_FirstValueRemains()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.Add(headers.Descriptor, rawPrefix + "2");

            // Remove the parsed value (note the original string 'raw2' was "parsed" to 'parsed2')
            Assert.True(headers.RemoveParsedValue(headers.Descriptor, parsedPrefix + "2"));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void RemoveParsedValue_RemoveFromNonExistingHeader_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix);

            // Header 'non-existing-header' can't be found, so false is returned.
            Assert.False(headers.RemoveParsedValue(customHeader, "doesntexist"));
        }

        [Fact]
        public void RemoveParsedValue_RemoveFromUninitializedHeaderStore_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();

            // If we never add a header value, the whole header (and also the header store) doesn't exist.
            // Make sure we considered this case.
            Assert.False(headers.RemoveParsedValue(headers.Descriptor, "doesntexist"));
        }

        [Fact]
        public void RemoveParsedValue_AddOneValueToKnownHeaderAndCompareWithValueThatDiffersInCase_CustomComparerUsedForComparison()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(headers.Descriptor, "value");

            // Our custom comparer (MockComparer) does case-insensitive value comparison. Verify that our custom
            // comparer is used to compare the header value.
            Assert.True(headers.RemoveParsedValue(headers.Descriptor, "VALUE"));
            Assert.False(headers.Contains(headers.Descriptor), "Header should be removed after removing value.");
            Assert.Equal(1, headers.Parser.MockComparer.EqualsCount);
        }

        [Fact]
        public void RemoveParsedValue_AddTwoValuesToKnownHeaderAndCompareWithValueThatDiffersInCase_CustomComparerUsedForComparison()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(headers.Descriptor, "differentvalue");
            headers.AddParsedValue(headers.Descriptor, "value");

            // Our custom comparer (MockComparer) does case-insensitive value comparison. Verify that our custom
            // comparer is used to compare the header value.
            // Note that since we added 2 values a different code path than in the previous test is used. In this
            // case we have stored the values as List<string> internally.
            Assert.True(headers.RemoveParsedValue(headers.Descriptor, "VALUE"));
            Assert.Equal(1, headers.GetValues(headers.Descriptor).Count());
            Assert.Equal(2, headers.Parser.MockComparer.EqualsCount);
        }

        [Fact]
        public void RemoveParsedValue_FirstAddNewlineCharsValueThenCallRemoveParsedValue_InvalidValueRemains()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with newline chars.
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue + "\r\ninvalid");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            headers.RemoveParsedValue(headers.Descriptor, "");

            Assert.True(headers.Contains(headers.Descriptor));
            Assert.Equal(new[] { invalidHeaderValue + "\r\ninvalid" }, headers.GetValues(headers.Descriptor));
        }

        [Fact]
        public void RemoveParsedValue_FirstAddNewlineCharsValueThenAddValidValueThenCallAddParsedValue_HeaderNotRemoved()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with newline chars.
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue + "\r\ninvalid");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            headers.RemoveParsedValue(headers.Descriptor, parsedPrefix + "1");

            Assert.True(headers.Contains(headers.Descriptor));
        }

        [Fact]
        public void Clear_AddMultipleHeadersAndThenClear_NoHeadersInCollection()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");
            headers.Add("custom1", "customValue1");
            headers.Add("custom2", "customValue2");
            headers.Add("custom3", "customValue3");

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // We added 4 different headers
            Assert.Equal(4, headers.Count());

            headers.Clear();

            Assert.Equal(0, headers.Count());

            // The call to Count() triggers a TryParseValue for the TryAddWithoutValidation() value. Clear() should
            // not cause any additional parsing operations.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Theory]
        [InlineData(null)]
        public void Remove_UseNullHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            AssertExtensions.Throws<ArgumentNullException>("name", () => { headers.Remove(headerName); });
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t\r\n ")]
        public void Remove_UseWhiteSpaceHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            AssertExtensions.Throws<ArgumentException>("name", () => { headers.Remove(headerName); });
        }

        [Theory]
        [MemberData(nameof(GetInvalidHeaderNames))]
        public void Remove_UseInvalidHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            Assert.Throws<FormatException>(() => { headers.Remove(headerName); });
        }

        [Fact]
        public void Remove_AddMultipleHeadersAndDeleteFirstAndLast_FirstAndLastHeaderRemoved()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");
            headers.Add("custom1", "customValue1");
            headers.Add("custom2", "customValue2");
            headers.Add("lastheader", "customValue3");

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // We added 4 different headers
            Assert.Equal(4, headers.Count());

            // Remove first header
            Assert.True(headers.Remove(headers.Descriptor));
            Assert.Equal(3, headers.Count());

            // Remove last header
            Assert.True(headers.Remove("lastheader"));
            Assert.Equal(2, headers.Count());

            // The call to Count() triggers a TryParseValue for the TryAddWithoutValidation() value. Clear() should
            // not cause any additional parsing operations.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Remove_RemoveHeaderFromUninitializedHeaderStore_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();

            // Remove header from uninitialized store (store collection is null)
            Assert.False(headers.Remove(headers.Descriptor));
            Assert.Equal(0, headers.Count());
        }

        [Fact]
        public void Remove_RemoveNonExistingHeader_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");

            Assert.Equal(1, headers.Count());

            // Remove header from empty store
            Assert.False(headers.Remove("doesntexist"));
            Assert.Equal(1, headers.Count());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void TryGetValues_UseEmptyHeaderName_False(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            IEnumerable<string> values = null;

            Assert.False(headers.TryGetValues(headerName, out values));
        }

        [Theory]
        [MemberData(nameof(GetInvalidHeaderNames))]
        public void TryGetValues_UseInvalidHeaderName_False(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            IEnumerable<string> values = null;

            Assert.False(headers.TryGetValues(headerName, out values));
        }

        [Fact]
        public void TryGetValues_GetValuesFromUninitializedHeaderStore_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();

            IEnumerable<string> values = null;

            // Get header values from uninitialized store (store collection is null)
            Assert.False(headers.TryGetValues("doesntexist", out values));
            Assert.Equal(0, headers.Count());
        }

        [Fact]
        public void TryGetValues_GetValuesForNonExistingHeader_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");

            IEnumerable<string> values = null;

            // Get header values from uninitialized store (store collection is null)
            Assert.False(headers.TryGetValues("doesntexist", out values));
            Assert.Equal(1, headers.Count());
        }

        [Fact]
        public void TryGetValues_GetValuesForExistingHeader_ReturnsTrueAndListOfValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");
            headers.TryAddWithoutValidation(headers.Descriptor, string.Empty);

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            IEnumerable<string> values = null;

            Assert.True(headers.TryGetValues(headers.Descriptor, out values));
            Assert.NotNull(values);

            // TryGetValues() should trigger parsing of values added with TryAddWithoutValidation()
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);

            // The first 2 valid values should be present.
            // The last empty value will be ignored.
            Assert.Equal(2, values.Count());

            // Check returned values
            Assert.Equal(parsedPrefix + "1", values.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", values.ElementAt(1));
        }

        [Theory]
        [InlineData(null)]
        public void GetValues_UseNullHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            AssertExtensions.Throws<ArgumentNullException>("name", () => { headers.GetValues(headerName); });
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t\r\n ")]
        public void GetValues_UseWhiteSpaceHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            AssertExtensions.Throws<ArgumentException>("name", () => { headers.GetValues(headerName); });
        }

        [Theory]
        [MemberData(nameof(GetInvalidHeaderNames))]
        public void GetValues_UseInvalidHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            Assert.Throws<FormatException>(() => { headers.GetValues(headerName); });
        }

        [Fact]
        public void GetValues_GetValuesFromUninitializedHeaderStore_Throw()
        {
            MockHeaders headers = new MockHeaders();

            // Get header values from uninitialized store (store collection is null). This will throw.
            Assert.Throws<InvalidOperationException>(() => { headers.GetValues("doesntexist"); });
        }

        [Fact]
        public void GetValues_GetValuesForNonExistingHeader_Throw()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");

            // Get header values for non-existing header (but other headers exist in the store).
            Assert.Throws<InvalidOperationException>(() => { headers.GetValues("doesntexist"); });
        }

        [Fact]
        public void GetValues_GetValuesForExistingHeader_ReturnsTrueAndListOfValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation("custom", rawPrefix + "0"); // this must not influence the result.
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            IEnumerable<string> values = headers.GetValues(headers.Descriptor);
            Assert.NotNull(values);

            // GetValues() should trigger parsing of values added with TryAddWithoutValidation()
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(2, values.Count());

            // Check returned values
            Assert.Equal(parsedPrefix + "1", values.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", values.ElementAt(1));
        }

        [Fact]
        public void GetValues_HeadersWithEmptyValues_ReturnsEmptyArray()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(customHeaderName, (string)null);
            headers.Add(headers.Descriptor, string.Empty);

            // In the known header case, the MockParser accepts empty values but tells the store to not add the value.
            // Since no value is added for 'knownHeader', HttpHeaders removes the header from the store. This is only
            // done for known headers. Custom headers are allowed to have empty/null values as shown by
            // 'valuesForCustomHeaders' below
            Assert.False(headers.Contains(headers.Descriptor));

            // In the custom header case, we add whatever the users adds (besides that we add string.Empty if the
            // user adds null). So here we do have 1 value: string.Empty.
            IEnumerable<string> valuesForCustomHeader = headers.GetValues(customHeaderName);
            Assert.NotNull(valuesForCustomHeader);
            Assert.Equal(1, valuesForCustomHeader.Count());
            Assert.Equal(string.Empty, valuesForCustomHeader.First());
        }

        [Fact]
        public void GetSingleParsedValue_GetValuesFromUninitializedHeaderStore_ReturnsNull()
        {
            MockHeaders headers = new MockHeaders();

            // Get header values from uninitialized store (store collection is null).
            object storeValue = headers.GetSingleParsedValue(customHeader);
            Assert.Null(storeValue);
        }

        [Fact]
        public void GetSingleParsedValue_GetValuesForNonExistingHeader_ReturnsNull()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");

            // Get header values for non-existing header (but other headers exist in the store).
            object storeValue = headers.GetSingleParsedValue(customHeader);
            Assert.Null(storeValue);
        }

        [Fact]
        public void GetSingleParsedValue_GetSingleValueForExistingHeader_ReturnsAddedValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(customHeader.Name, "customValue1");

            // Get header values for non-existing header (but other headers exist in the store).
            object storeValue = headers.GetSingleParsedValue(customHeader);
            Assert.NotNull(storeValue);

            // If we only have one value, then GetValues() should return just the value and not wrap it in a List<T>.
            Assert.Equal("customValue1", storeValue);
        }

        [Fact]
        public void GetSingleParsedValue_HeaderWithEmptyValues_ReturnsEmpty()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, string.Empty);

            object storeValue = headers.GetSingleParsedValue(headers.Descriptor);
            Assert.Null(storeValue);
        }

        [Fact]
        public void GetSingleParsedValue_GetMultipleValuesForExistingHeader_ReturnsListOfValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation("custom", rawPrefix + "0"); // this must not influence the result.
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            object storeValue = headers.GetParsedAndInvalidValues(headers.Descriptor);
            Assert.NotNull(storeValue);

            // GetValues<T>() should trigger parsing of values added with TryAddWithoutValidation()
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            // Since we added 2 values to header 'knownHeader', we expect GetValues() to return a List<T> with
            // two values.
            List<object> storeValues = storeValue as List<object>;
            Assert.NotNull(storeValues);
            Assert.Equal(2, storeValues.Count);
            Assert.Equal(parsedPrefix + "1", storeValues[0]);
            Assert.Equal(parsedPrefix + "2", storeValues[1]);
        }

        [Fact]
        public void GetSingleParsedValue_GetValuesForExistingHeaderWithInvalidValues_ReturnsOnlyParsedValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix);

            // Here we add an invalid value. GetValues<T> only returns parsable values. So this value should get
            // parsed, however it will be added to the 'invalid values' list and thus is not part of the collection
            // returned by the enumerator.
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue);

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            object storeValue = headers.GetSingleParsedValue(headers.Descriptor);
            Assert.NotNull(storeValue);

            // GetValues<T>() should trigger parsing of values added with TryAddWithoutValidation()
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            // Since we added only one valid value to 'knownHeader', we expect GetValues() to return a that value.
            Assert.Equal(parsedPrefix, storeValue);
        }

        [Fact]
        public void GetSingleParsedValue_GetValuesForExistingHeaderWithOnlyInvalidValues_ReturnsEmptyEnumerator()
        {
            MockHeaders headers = new MockHeaders();

            // Here we add an invalid value. GetValues<T> only returns parsable values. So this value should get
            // parsed, however it will be added to the 'invalid values' list and thus is not part of the collection
            // returned by the enumerator.
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            object storeValue = headers.GetSingleParsedValue(headers.Descriptor);
            Assert.Null(storeValue);

            // GetValues<T>() should trigger parsing of values added with TryAddWithoutValidation()
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void GetSingleParsedValue_AddInvalidValueToHeader_HeaderNotRemovedAndNullReturned()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue + "\r\ninvalid");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            object storeValue = headers.GetSingleParsedValue(headers.Descriptor);
            Assert.Null(storeValue);
            Assert.True(headers.Contains(headers.Descriptor));
        }

        [Fact]
        public void GetSingleParsedValue_GetSingleParsedValueForKnownHeaderWithNewlineChars_ReturnsNull()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with newline chars.
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue + "\r\ninvalid");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
            Assert.Null(headers.GetSingleParsedValue(headers.Descriptor));
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void NonValidated_Default_Empty()
        {
            HttpHeadersNonValidated v = default;
            Assert.Equal(0, v.Count);
            Assert.Empty(v);
            Assert.False(v.TryGetValues("Host", out HeaderStringValues values));
            Assert.Empty(values);
        }

        [Fact]
        public void NonValidated_SetValidAndInvalidHeaderValues_AllHeaderValuesReturned()
        {
            MockHeaderParser parser = new MockHeaderParser("---");
            MockHeaders headers = new MockHeaders(parser);

            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, "value2,value3");
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue);

            string expectedValue = $"{parsedPrefix}1---value2,value3---{invalidHeaderValue}";

            Assert.Equal(1, headers.NonValidated.Count);

            int iterations = 0;
            foreach (KeyValuePair<string, HeaderStringValues> header in headers.NonValidated)
            {
                // Note that raw values don't get parsed but just added to the result.
                iterations++;
                Assert.Equal(headers.Descriptor.Name, header.Key);
                Assert.Equal(3, header.Value.Count);
                Assert.Equal(expectedValue, header.Value.ToString());
            }

            Assert.Equal(1, iterations);
        }

        [Fact]
        public void NonValidated_SetMultipleHeaders_AllHeaderValuesReturned()
        {
            MockHeaderParser parser = new MockHeaderParser(true);
            MockHeaders headers = new MockHeaders(parser);

            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.Add("header2", "value2");
            headers.Add("header3", (string)null);
            headers.Add("header4", "value41");
            headers.Add("header4", "value42");

            string[] expectedHeaderNames = { headers.Descriptor.Name, "header2", "header3", "header4" };
            string[] expectedHeaderValues = { parsedPrefix + "1", "value2", "", "value41, value42" };
            int i = 0;

            foreach (KeyValuePair<string, HeaderStringValues> header in headers.NonValidated)
            {
                Assert.NotEqual(expectedHeaderNames.Length, i);
                Assert.Equal(expectedHeaderNames[i], header.Key);
                Assert.Equal(expectedHeaderValues[i], header.Value.ToString());
                i++;
            }
        }

        [Fact]
        public void NonValidated_SetMultipleValuesOnSingleValueHeader_AllHeaderValuesReturned()
        {
            MockHeaderParser parser = new MockHeaderParser(false);
            MockHeaders headers = new MockHeaders(parser);

            headers.TryAddWithoutValidation(headers.Descriptor, "value1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix);

            foreach (KeyValuePair<string, HeaderStringValues> header in headers.NonValidated)
            {
                Assert.Equal(headers.Descriptor.Name, header.Key);
                // Note that the added rawPrefix did not get parsed
                Assert.Equal("value1, " + rawPrefix, header.Value.ToString());
            }
        }

        [Fact]
        public void NonValidated_ValidAndInvalidValues_DictionaryMembersWork()
        {
            var headers = new HttpResponseHeaders();
            IReadOnlyDictionary<string, HeaderStringValues> nonValidated = headers.NonValidated;

            Assert.True(headers.TryAddWithoutValidation("Location", "http:/invalidLocation"));
            Assert.True(headers.TryAddWithoutValidation("Location", "http:/anotherLocation"));
            Assert.True(headers.TryAddWithoutValidation("Date", "not a date"));

            Assert.Equal(2, nonValidated.Count);

            Assert.True(nonValidated.ContainsKey("Location"));
            Assert.True(nonValidated.ContainsKey("Date"));

            Assert.False(nonValidated.ContainsKey("Age"));
            Assert.False(nonValidated.TryGetValue("Age", out _));
            Assert.Throws<KeyNotFoundException>(() => nonValidated["Age"]);

            Assert.True(nonValidated.TryGetValue("Location", out HeaderStringValues locations));
            Assert.Equal(2, locations.Count);
            Assert.Equal(new[] { "http:/invalidLocation", "http:/anotherLocation" }, locations.ToArray());
            Assert.Equal("http:/invalidLocation, http:/anotherLocation", locations.ToString());

            Assert.True(nonValidated.TryGetValue("Date", out HeaderStringValues dates));
            Assert.Equal(1, dates.Count);
            Assert.Equal(new[] { "not a date" }, dates.ToArray());
            Assert.Equal("not a date", dates.ToString());

            dates = nonValidated["Date"];
            Assert.Equal(1, dates.Count);
            Assert.Equal(new[] { "not a date" }, dates.ToArray());
            Assert.Equal("not a date", dates.ToString());

            Assert.Equal(new HashSet<string> { "Location", "Date" }, nonValidated.Keys.ToHashSet());
        }

        [Fact]
        public void NonValidated_ValidInvalidAndRaw_AllReturned()
        {
            var headers = new HttpResponseHeaders();
            IReadOnlyDictionary<string, HeaderStringValues> nonValidated = headers.NonValidated;

            // Parsed value
            headers.Date = new DateTimeOffset(1, 2, 3, 4, 5, 6, TimeSpan.Zero);

            // Invalid value
            headers.TryAddWithoutValidation("Date", "not a date");
            foreach (KeyValuePair<string, IEnumerable<string>> _ in headers) { }

            // Raw value
            headers.TryAddWithoutValidation("Date", "another not a date");

            // All three show up
            Assert.Equal(1, nonValidated.Count);
            Assert.Equal(3, nonValidated["Date"].Count);
            using (new ThreadCultureChange(new CultureInfo("en-US")))
            {
                Assert.Equal(new HashSet<string> { "not a date", "another not a date", "Sat, 03 Feb 0001 04:05:06 GMT" }, nonValidated["Date"].ToHashSet());
            }
        }

        [Theory]
        [InlineData(null)]
        public void Contains_UseNullHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            AssertExtensions.Throws<ArgumentNullException>("name", () => { headers.Contains(headerName); });
        }

        [Theory]
        [InlineData("")]
        [InlineData(" \t\r\n ")]
        public void Contains_UseEmptyHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            AssertExtensions.Throws<ArgumentException>("name", () => { headers.Contains(headerName); });
        }

        [Theory]
        [MemberData(nameof(GetInvalidHeaderNames))]
        public void Contains_UseInvalidHeaderName_Throw(string headerName)
        {
            MockHeaders headers = new MockHeaders();

            Assert.Throws<FormatException>(() => { headers.Contains(headerName); });
        }

        [Fact]
        public void Contains_CallContainsFromUninitializedHeaderStore_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            Assert.False(headers.Contains("doesntexist"));
        }

        [Fact]
        public void Contains_CallContainsForNonExistingHeader_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix);
            Assert.False(headers.Contains("doesntexist"));
        }

        [Fact]
        public void Contains_CallContainsForEmptyHeader_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, string.Empty);
            Assert.False(headers.Contains(headers.Descriptor));
        }

        [Fact]
        public void Contains_CallContainsForExistingHeader_ReturnsTrue()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");
            headers.Add("custom2", "customValue2");
            headers.Add("custom3", "customValue3");
            headers.Add("custom4", "customValue4");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix);

            // Nothing got parsed so far since we just added custom headers and for the known header we called
            // TryAddWithoutValidation().
            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.True(headers.Contains(headers.Descriptor));

            // Contains() should trigger parsing of values added with TryAddWithoutValidation(): If the value was invalid,
            // i.e. contains newline chars, then the header will not be removed from the collection.
            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Contains_AddValuesWithNewlineChars_HeadersNotRemovedWhenCallingContains()
        {
            MockHeaders headers = new MockHeaders();

            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue + "\r\ninvalid");
            headers.TryAddWithoutValidation("custom", "invalid\r\nvalue");

            Assert.True(headers.Contains(headers.Descriptor));
            Assert.True(headers.Contains("custom"));
        }

        [Fact]
        public void GetEnumerator_GetEnumeratorFromUninitializedHeaderStore_ReturnsEmptyEnumerator()
        {
            MockHeaders headers = new MockHeaders();

            IEnumerator<KeyValuePair<string, IEnumerable<string>>> enumerator = headers.GetEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void GetEnumerator_FirstHeaderWithOneValueSecondHeaderWithTwoValues_EnumeratorReturnsTwoHeaders()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(customHeaderName, "custom0");
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            // The value added with TryAddWithoutValidation() wasn't parsed yet.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            IEnumerator<KeyValuePair<string, IEnumerable<string>>> enumerator = headers.GetEnumerator();

            // Getting the enumerator doesn't trigger parsing.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            Assert.True(enumerator.MoveNext());
            Assert.Equal(customHeaderName, enumerator.Current.Key);
            Assert.Equal(1, enumerator.Current.Value.Count());
            Assert.Equal("custom0", enumerator.Current.Value.ElementAt(0));

            // Starting using the enumerator will trigger parsing of raw values. The first header is not a known
            // header, so there shouldn't be any parsing.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            Assert.True(enumerator.MoveNext());
            Assert.Equal(headers.Descriptor.Name, enumerator.Current.Key);
            Assert.Equal(2, enumerator.Current.Value.Count());
            Assert.Equal(parsedPrefix + "1", enumerator.Current.Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", enumerator.Current.Value.ElementAt(1));

            // The second header is a known header, so parsing raw values should get executed.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.False(enumerator.MoveNext(), "Only 2 values expected, but enumerator returns a third one.");
        }

        [Fact]
        public void GetEnumerator_FirstCustomHeaderWithEmptyValueSecondKnownHeaderWithEmptyValue_EnumeratorReturnsOneHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(customHeaderName, string.Empty);
            headers.Add(headers.Descriptor, string.Empty);

            IEnumerator<KeyValuePair<string, IEnumerable<string>>> enumerator = headers.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(customHeaderName, enumerator.Current.Key);
            Assert.Equal(1, enumerator.Current.Value.Count());
            Assert.Equal(string.Empty, enumerator.Current.Value.ElementAt(0));

            Assert.False(enumerator.MoveNext(), "Only the (empty) custom value should be returned.");
        }

        [Fact]
        public void GetEnumerator_UseExplicitInterfaceImplementation_EnumeratorReturnsNoOfHeaders()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");
            headers.Add("custom2", "customValue2");
            headers.Add("custom3", "customValue3");
            headers.Add("custom4", "customValue4");

            System.Collections.IEnumerable headersAsIEnumerable = headers;

            IEnumerator enumerator = headersAsIEnumerable.GetEnumerator();

            KeyValuePair<string, IEnumerable<string>> currentValue;

            for (int i = 1; i <= 4; i++)
            {
                Assert.True(enumerator.MoveNext());
                currentValue = (KeyValuePair<string, IEnumerable<string>>)enumerator.Current;
                Assert.Equal("custom" + i, currentValue.Key);
                Assert.Equal(1, currentValue.Value.Count());
            }

            Assert.False(enumerator.MoveNext(), "Only 2 values expected, but enumerator returns a third one.");
        }

        [Fact]
        public void GetEnumerator_InvalidValueBetweenValidHeaders_EnumeratorReturnsAllValidValuesAndDoesNotRemoveInvalidValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation("foo", "fooValue");
            headers.TryAddWithoutValidation("invalid", "invalid\nvalue");
            headers.TryAddWithoutValidation("bar", "barValue");

            Assert.Equal(3, headers.Count);

            IDictionary<string, IEnumerable<string>> dict = headers.ToDictionary(pair => pair.Key, pair => pair.Value);
            Assert.Equal("fooValue", Assert.Single(Assert.Contains("foo", dict)));
            Assert.Equal("barValue", Assert.Single(Assert.Contains("bar", dict)));

            Assert.Equal(3, headers.Count);
            Assert.True(headers.NonValidated.Contains("invalid"));
        }

        [Fact]
        public void AddParsedValue_AddSingleValueToNonExistingHeader_HeaderGetsCreatedAndValueAdded()
        {
            Uri headerValue = new Uri("http://example.org/");

            CustomTypeHeaders headers = new CustomTypeHeaders();
            headers.AddParsedValue(customTypeHeader, headerValue);

            Assert.True(headers.Contains(customTypeHeader), "Store doesn't have the header after adding a value to it.");

            Assert.Equal(headerValue.ToString(), headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void AddParsedValue_AddValueTypeValueToNonExistingHeader_HeaderGetsCreatedAndBoxedValueAdded()
        {
            int headerValue = 5;

            CustomTypeHeaders headers = new CustomTypeHeaders();
            headers.AddParsedValue(customTypeHeader, headerValue);

            Assert.True(headers.Contains(customTypeHeader), "Store doesn't have the header after adding a value to it.");

            Assert.Equal(headerValue.ToString(), headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void AddParsedValue_AddTwoValuesToNonExistingHeader_HeaderGetsCreatedAndValuesAdded()
        {
            Uri headerValue1 = new Uri("http://example.org/1/");
            Uri headerValue2 = new Uri("http://example.org/2/");

            CustomTypeHeaders headers = new CustomTypeHeaders();
            headers.AddParsedValue(customTypeHeader, headerValue1);

            // Adding a second value will cause a List<T> to be created in order to store values. If we just add
            // one value, no List<T> is created, but the header is just added as store value.
            headers.AddParsedValue(customTypeHeader, headerValue2);

            Assert.True(headers.Contains(customTypeHeader), "Store doesn't have the header after adding a value to it.");
            Assert.Equal(2, headers.GetValues(customTypeHeader).Count());

            Assert.Equal(headerValue1.ToString(), headers.First().Value.ElementAt(0));
            Assert.Equal(headerValue2.ToString(), headers.First().Value.ElementAt(1));
        }

        [Fact]
        public void AddParsedValue_UseDifferentAddMethods_AllValuesAddedCorrectly()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            headers.AddParsedValue(headers.Descriptor, parsedPrefix + "3");

            // Adding a parsed value, will trigger all raw values to be parsed.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(3, headers.GetValues(headers.Descriptor).Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(parsedPrefix + "3", headers.First().Value.ElementAt(2));
        }

        [Fact]
        public void AddParsedValue_FirstAddNewlineCharsValueThenCallAddParsedValue_ParsedValueAdded()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with newline chars.
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue + "\r\ninvalid");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            headers.AddParsedValue(headers.Descriptor, parsedPrefix + "1");

            Assert.True(headers.Contains(headers.Descriptor), "Store should have an entry for 'knownHeader'.");
            Assert.Equal(new[] { invalidHeaderValue + "\r\ninvalid", parsedPrefix + "1" }, headers.GetValues(headers.Descriptor));
        }

        [Fact]
        public void AddParsedValue_FirstAddNewlineCharsValueThenAddValidValueThenCallAddParsedValue_ParsedValueAdded()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with newline chars.
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue + "\r\ninvalid");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "0");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            headers.AddParsedValue(headers.Descriptor, parsedPrefix + "1");

            Assert.True(headers.Contains(headers.Descriptor), "Store should have an entry for 'knownHeader'.");
            Assert.Equal(3, headers.GetValues(headers.Descriptor).Count());
            Assert.Equal(parsedPrefix + "0", headers.GetValues(headers.Descriptor).ElementAt(1));
            Assert.Equal(parsedPrefix + "1", headers.GetValues(headers.Descriptor).ElementAt(2));
        }

        [Fact]
        public void SetParsedValue_AddSingleValueToNonExistingHeader_HeaderGetsCreatedAndValueAdded()
        {
            Uri headerValue = new Uri("http://example.org/");

            CustomTypeHeaders headers = new CustomTypeHeaders();
            headers.SetParsedValue(customTypeHeader, headerValue);

            Assert.True(headers.Contains(customTypeHeader), "Store doesn't have the header after adding a value to it.");

            Assert.Equal(headerValue.ToString(), headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void SetParsedValue_SetTwoValuesToNonExistingHeader_HeaderGetsCreatedAndLastValueAdded()
        {
            Uri headerValue1 = new Uri("http://example.org/1/");
            Uri headerValue2 = new Uri("http://example.org/2/");

            CustomTypeHeaders headers = new CustomTypeHeaders();
            headers.SetParsedValue(customTypeHeader, headerValue1);

            // The following line will remove the previously added values and replace them with the provided value.
            headers.SetParsedValue(customTypeHeader, headerValue2);

            Assert.True(headers.Contains(customTypeHeader), "Store doesn't have the header after adding a value to it.");
            Assert.Equal(1, headers.GetValues(customTypeHeader).Count());

            // The second value replaces the first value.
            Assert.Equal(headerValue2.ToString(), headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void SetParsedValue_SetValueAfterAddingMultipleValues_SetValueReplacesOtherValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            headers.SetParsedValue(headers.Descriptor, parsedPrefix + "3");

            // Adding a parsed value, will trigger all raw values to be parsed.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.GetValues(headers.Descriptor).Count());
            Assert.Equal(parsedPrefix + "3", headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void ContainsParsedValue_ContainsParsedValueFromUninitializedHeaderStore_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            Assert.False(headers.ContainsParsedValue(customHeader, "custom1"));
        }

        [Fact]
        public void ContainsParsedValue_ContainsParsedValueForNonExistingHeader_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.False(headers.ContainsParsedValue(customHeader, "custom1"));

            // ContainsParsedValue() must not trigger raw value parsing for headers other than the requested one.
            // In this case we expect ContainsParsedValue(customeHeader) not to trigger raw value parsing for
            // 'headers.Descriptor'.
            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void ContainsParsedValue_ContainsParsedValueForNonExistingHeaderValue_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(headers.Descriptor, "value1");
            headers.AddParsedValue(headers.Descriptor, "value2");

            // After adding two values to header 'knownHeader' we ask for a non-existing value.
            Assert.False(headers.ContainsParsedValue(headers.Descriptor, "doesntexist"));
        }

        [Fact]
        public void ContainsParsedValue_ContainsParsedValueForExistingHeaderButNonAvailableValue_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.False(headers.ContainsParsedValue(headers.Descriptor, "custom1"));

            // ContainsParsedValue() must trigger raw value parsing for the header it was asked for.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void ContainsParsedValue_ContainsParsedValueForExistingHeaderWithAvailableValue_ReturnsTrue()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "1");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "2");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "3");
            headers.TryAddWithoutValidation(headers.Descriptor, rawPrefix + "4");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.True(headers.ContainsParsedValue(headers.Descriptor, parsedPrefix + "3"));

            // ContainsParsedValue() must trigger raw value parsing for the header it was asked for.
            Assert.Equal(4, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void ContainsParsedValue_AddOneValueToKnownHeaderAndCompareWithValueThatDiffersInCase_CustomComparerUsedForComparison()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(headers.Descriptor, "value");

            // Our custom comparer (MockComparer) does case-insensitive value comparison. Verify that our custom
            // comparer is used to compare the header value.
            Assert.True(headers.ContainsParsedValue(headers.Descriptor, "VALUE"));
            Assert.Equal(1, headers.Parser.MockComparer.EqualsCount);

            headers.Clear();
            headers.TryAddWithoutValidation(headers.Descriptor, invalidHeaderValue);
            Assert.False(headers.ContainsParsedValue(headers.Descriptor, invalidHeaderValue));
        }

        [Fact]
        public void ContainsParsedValue_AddTwoValuesToKnownHeaderAndCompareWithValueThatDiffersInCase_CustomComparerUsedForComparison()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(headers.Descriptor, "differentvalue");
            headers.AddParsedValue(headers.Descriptor, "value");

            // Our custom comparer (MockComparer) does case-insensitive value comparison. Verify that our custom
            // comparer is used to compare the header value.
            // Note that since we added 2 values a different code path than in the previous test is used. In this
            // case we have stored the values as List<string> internally.
            Assert.True(headers.ContainsParsedValue(headers.Descriptor, "VALUE"));
            Assert.Equal(2, headers.Parser.MockComparer.EqualsCount);
        }

        [Fact]
        public void ContainsParsedValue_ParserWithNoEqualityComparer_CaseSensitiveComparison()
        {
            CustomTypeHeaders headers = new CustomTypeHeaders();
            headers.AddParsedValue(noComparerHeader, "lowercasevalue");

            // Since we don't provide a comparer, the default string.Equals() is called which is case-sensitive. So
            // the following call should return false.
            Assert.False(headers.ContainsParsedValue(noComparerHeader, "LOWERCASEVALUE"));

            // Now we try to use the correct casing. This should return true.
            Assert.True(headers.ContainsParsedValue(noComparerHeader, "lowercasevalue"));
        }

        [Fact]
        public void ContainsParsedValue_CallFromEmptyHeaderStore_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();

            // This will create a header entry with no value.
            headers.Add(headers.Descriptor, string.Empty);

            Assert.False(headers.Contains(headers.Descriptor), "Expected known header to be in the store.");

            // This will just return fals and not touch the header.
            Assert.False(headers.ContainsParsedValue(headers.Descriptor, "x"),
                "Expected 'ContainsParsedValue' to return false.");
        }

        [Fact]
        public void AddHeaders_SourceAndDestinationStoreHaveMultipleHeaders_OnlyHeadersNotInDestinationAreCopiedFromSource()
        {
            // Add header values to the source store.
            MockHeaders source = new MockHeaders();
            source.Add("custom1", "source10");
            source.Add("custom1", "source11");

            source.TryAddWithoutValidation("custom2", "source2");

            source.Add(known1Header, rawPrefix + "3");
            source.TryAddWithoutValidation(known1Header, rawPrefix + "4");

            source.TryAddWithoutValidation(known2Header, rawPrefix + "5");
            source.TryAddWithoutValidation(known2Header, invalidHeaderValue);
            source.TryAddWithoutValidation(known2Header, rawPrefix + "7");

            // this header value gets removed when it gets parsed.
            source.TryAddWithoutValidation(known3Header, (string)null);
            source.Add(known3Header, string.Empty);

            DateTimeOffset known4Value1 = new DateTimeOffset(2010, 6, 15, 18, 31, 34, TimeSpan.Zero);
            DateTimeOffset known4Value2 = new DateTimeOffset(2010, 4, 8, 11, 21, 04, TimeSpan.Zero);
            source.AddParsedValue(known4Header, known4Value1);
            source.AddParsedValue(known4Header, known4Value2);

            source.Add("custom5", "source5");
            source.TryAddWithoutValidation("custom6", (string)null);

            // This header value gets added even though it doesn't have values. But since this is a custom header we
            // assume it supports empty values.
            source.TryAddWithoutValidation("custom7", (string)null);
            source.Add("custom7", string.Empty);

            // Add header values to the destination store.
            MockHeaders destination = new MockHeaders();
            destination.Add("custom2", "destination1");
            destination.Add(known1Header, rawPrefix + "9");

            // Now add all headers that are in source but not destination to destination.
            destination.AddHeaders(source);

            Assert.Equal(9, destination.Count());

            Assert.Equal(2, destination.GetValues("custom1").Count());
            Assert.Equal("source10", destination.GetValues("custom1").ElementAt(0));
            Assert.Equal("source11", destination.GetValues("custom1").ElementAt(1));

            // This value was set in destination. The header in source was ignored.
            Assert.Equal(1, destination.GetValues("custom2").Count());
            Assert.Equal("destination1", destination.GetValues("custom2").First());

            // This value was set in destination. The header in source was ignored.
            Assert.Equal(1, destination.GetValues(known1Header).Count());
            Assert.Equal(parsedPrefix + "9", destination.GetValues(known1Header).First());

            // The header in source gets first parsed and then copied to destination. Note that here we have one
            // invalid value.
            Assert.Equal(3, destination.GetValues(known2Header).Count());
            Assert.Equal(parsedPrefix + "5", destination.GetValues(known2Header).ElementAt(0));
            Assert.Equal(invalidHeaderValue, destination.GetValues(known2Header).ElementAt(1));
            Assert.Equal(parsedPrefix + "7", destination.GetValues(known2Header).ElementAt(2));
            // Header 'known3' should be copied, since parsing should not remove any values.
            Assert.True(destination.Contains(known3Header), "'known3' header value count.");

            Assert.Equal(2, destination.GetValues(known4Header).Count());
            Assert.Equal(known4Value1.ToString(), destination.GetValues(known4Header).ElementAt(0));
            Assert.Equal(known4Value2.ToString(), destination.GetValues(known4Header).ElementAt(1));

            Assert.Equal("source5", destination.GetValues("custom5").First());

            Assert.Equal(string.Empty, destination.GetValues("custom6").First());

            // Unlike 'known3', 'custom7' was added even though it only had empty values. The reason is that 'custom7'
            // is a custom header so we just add whatever value we get passed in.
            Assert.Equal(2, destination.GetValues("custom7").Count());
            Assert.Equal("", destination.GetValues("custom7").ElementAt(0));
            Assert.Equal("", destination.GetValues("custom7").ElementAt(1));
        }

        [Fact]
        public void AddHeaders_SourceHasEmptyHeaderStore_DestinationRemainsUnchanged()
        {
            MockHeaders source = new MockHeaders();

            MockHeaders destination = new MockHeaders();
            destination.Add(known1Header, rawPrefix);

            destination.AddHeaders(source);

            Assert.Equal(1, destination.Count());
        }

        [Fact]
        public void AddHeaders_DestinationHasEmptyHeaderStore_DestinationHeaderStoreGetsCreatedAndValuesAdded()
        {
            MockHeaders source = new MockHeaders();
            source.Add(known1Header, rawPrefix);

            MockHeaders destination = new MockHeaders();

            destination.AddHeaders(source);

            Assert.Equal(1, destination.Count());
        }

        [Fact]
        public void AddHeaders_SourceHasInvalidHeaderValues_InvalidHeadersNotRemovedFromSourceAndCopiedToDestination()
        {
            MockHeaders source = new MockHeaders();
            source.TryAddWithoutValidation(known1Header, invalidHeaderValue + "\r\ninvalid");
            source.TryAddWithoutValidation("custom", "invalid\r\nvalue");

            MockHeaders destination = new MockHeaders();
            destination.AddHeaders(source);

            Assert.Equal(2, source.Count());
            Assert.True(source.Contains(known1Header));
            Assert.True(source.Contains("custom"));
            Assert.Equal(2, destination.Count());
            Assert.True(destination.Contains(known1Header));
            Assert.True(destination.Contains("custom"));
        }

        [Fact]
        public void AddHeaders_ResponseHeaderToRequestHeaders_Success()
        {
            const string Name = "WWW-Authenticate";
            const string Value = "Basic realm=\"Access to the staging site\", charset=\"UTF-8\"";

            var request = new HttpRequestMessage();
            Assert.True(request.Headers.TryAddWithoutValidation(Name, Value));

            Assert.True(request.Headers.Contains(Name));
            Assert.True(request.Headers.NonValidated.Contains(Name));

            Assert.True(request.Headers.TryGetValues(Name, out IEnumerable<string> values));
            Assert.Equal(Value, values.Single());

            Assert.True(request.Headers.NonValidated.TryGetValues(Name, out HeaderStringValues nvValues));
            Assert.Equal(Value, nvValues.Single());
        }

        [Fact]
        public void AddHeaders_RequestHeaderToResponseHeaders_Success()
        {
            const string Name = "Referer";
            const string Value = "https://dot.net";

            var response = new HttpResponseMessage();
            Assert.True(response.Headers.TryAddWithoutValidation(Name, Value));

            Assert.True(response.Headers.Contains(Name));
            Assert.True(response.Headers.NonValidated.Contains(Name));

            Assert.True(response.Headers.TryGetValues(Name, out IEnumerable<string> values));
            Assert.Equal(Value, values.Single());

            Assert.True(response.Headers.NonValidated.TryGetValues(Name, out HeaderStringValues nvValues));
            Assert.Equal(Value, nvValues.Single());
        }

        [Fact]
        public void HeaderStringValues_Default_Empty()
        {
            HeaderStringValues v = default;
            Assert.Equal(0, v.Count);
            Assert.Empty(v);
            Assert.Equal(string.Empty, v.ToString());
        }

        [Fact]
        public void HeaderStringValues_Constructed_ProducesExpectedResults()
        {
            // 0 strings
            foreach (HeaderStringValues hsv in new[] { new HeaderStringValues(KnownHeaders.Accept.Descriptor, Array.Empty<string>()) })
            {
                Assert.Equal(0, hsv.Count);

                HeaderStringValues.Enumerator e = hsv.GetEnumerator();

                Assert.False(e.MoveNext());

                Assert.Equal(string.Empty, hsv.ToString());
            }

            // 1 string
            foreach (HeaderStringValues hsv in new[] { new HeaderStringValues(KnownHeaders.Accept.Descriptor, "hello"), new HeaderStringValues(KnownHeaders.Accept.Descriptor, new[] { "hello" }) })
            {
                Assert.Equal(1, hsv.Count);

                HeaderStringValues.Enumerator e = hsv.GetEnumerator();

                Assert.True(e.MoveNext());
                Assert.Equal("hello", e.Current);

                Assert.False(e.MoveNext());

                Assert.Equal("hello", hsv.ToString());
            }

            // 2 strings
            foreach (HeaderStringValues hsv in new[] { new HeaderStringValues(KnownHeaders.Accept.Descriptor, new[] { "hello", "world" }) })
            {
                Assert.Equal(2, hsv.Count);

                HeaderStringValues.Enumerator e = hsv.GetEnumerator();

                Assert.True(e.MoveNext());
                Assert.Equal("hello", e.Current);

                Assert.True(e.MoveNext());
                Assert.Equal("world", e.Current);

                Assert.False(e.MoveNext());

                Assert.Equal("hello, world", hsv.ToString());
            }
        }

        [Theory]
        [MemberData(nameof(NumberOfHeadersUpToArrayThreshold_AddNonValidated_EnumerateNonValidated))]
        public void Add_WithinArrayThresholdHeaders_EnumerationPreservesOrdering(int numberOfHeaders, bool addNonValidated, bool enumerateNonValidated)
        {
            var headers = new MockHeaders();

            for (int i = 0; i < numberOfHeaders; i++)
            {
                if (addNonValidated)
                {
                    headers.TryAddWithoutValidation(i.ToString(), i.ToString());
                }
                else
                {
                    headers.Add(i.ToString(), i.ToString());
                }
            }

            KeyValuePair<string, string>[] entries = enumerateNonValidated
                ? headers.NonValidated.Select(pair => KeyValuePair.Create(pair.Key, Assert.Single(pair.Value))).ToArray()
                : headers.Select(pair => KeyValuePair.Create(pair.Key, Assert.Single(pair.Value))).ToArray();

            Assert.Equal(numberOfHeaders, entries.Length);
            for (int i = 0; i < numberOfHeaders; i++)
            {
                Assert.Equal(i.ToString(), entries[i].Key);
                Assert.Equal(i.ToString(), entries[i].Value);
            }
        }

        [Fact]
        public void Add_Remove_HeaderOrderingIsPreserved()
        {
            var headers = new MockHeaders();
            headers.Add("a", "");
            headers.Add("b", "");
            headers.Add("c", "");

            headers.Remove("b");

            Assert.Equal(new[] { "a", "c" }, headers.Select(pair => pair.Key));
        }

        [Fact]
        public void Add_AddToExistingKey_OriginalOrderingIsPreserved()
        {
            var headers = new MockHeaders();
            headers.Add("a", "a1");
            headers.Add("b", "b1");
            headers.Add("a", "a2");

            Assert.Equal(new[] { "a", "b" }, headers.Select(pair => pair.Key));
        }

        [Theory]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(HttpHeaders.ArrayThreshold / 4)]
        [InlineData(HttpHeaders.ArrayThreshold / 2)]
        [InlineData(HttpHeaders.ArrayThreshold - 1)]
        [InlineData(HttpHeaders.ArrayThreshold)]
        [InlineData(HttpHeaders.ArrayThreshold + 1)]
        [InlineData(HttpHeaders.ArrayThreshold * 2)]
        [InlineData(HttpHeaders.ArrayThreshold * 4)]
        public void Add_LargeNumberOfHeaders_OperationsStillSupported(int numberOfHeaders)
        {
            string[] keys = Enumerable.Range(1, numberOfHeaders).Select(i => i.ToString()).ToArray();

            var headers = new MockHeaders();
            foreach (string key in keys)
            {
                Assert.False(headers.NonValidated.Contains(key));
                headers.TryAddWithoutValidation(key, key);
                Assert.True(headers.NonValidated.Contains(key));
            }

            string[] nonValidatedKeys = headers.NonValidated.Select(pair => pair.Key).ToArray();
            Assert.Equal(numberOfHeaders, nonValidatedKeys.Length);

            string[] newKeys = headers.Select(pair => pair.Key).ToArray();
            Assert.Equal(numberOfHeaders, newKeys.Length);

            string[] nonValidatedKeysAfterValidation = headers.NonValidated.Select(pair => pair.Key).ToArray();
            Assert.Equal(numberOfHeaders, nonValidatedKeysAfterValidation.Length);

            if (numberOfHeaders > HttpHeaders.ArrayThreshold)
            {
                // Ordering is lost when adding more than ArrayThreshold headers
                Array.Sort(nonValidatedKeys, (a, b) => int.Parse(a).CompareTo(int.Parse(b)));
                Array.Sort(newKeys, (a, b) => int.Parse(a).CompareTo(int.Parse(b)));
                Array.Sort(nonValidatedKeysAfterValidation, (a, b) => int.Parse(a).CompareTo(int.Parse(b)));
            }
            Assert.Equal(keys, nonValidatedKeys);
            Assert.Equal(keys, newKeys);
            Assert.Equal(keys, nonValidatedKeysAfterValidation);

            headers.Add("3", "secondValue");
            Assert.True(headers.TryGetValues("3", out IEnumerable<string> valuesFor3));
            Assert.Equal(new[] { "3", "secondValue" }, valuesFor3);

            Assert.True(headers.TryAddWithoutValidation("invalid", "invalid\nvalue"));
            Assert.True(headers.TryAddWithoutValidation("valid", "validValue"));

            Assert.Equal(numberOfHeaders + 2, headers.NonValidated.Count);

            // Remove all headers except for "1", "valid", "invalid"
            for (int i = 2; i <= numberOfHeaders; i++)
            {
                Assert.True(headers.Remove(i.ToString()));
            }

            Assert.False(headers.Remove("3"));

            // "1", "invalid", "valid"
            Assert.True(headers.NonValidated.Contains("invalid"));
            Assert.Equal(3, headers.NonValidated.Count);

            Assert.Equal(new[] { "1", "invalid", "valid" }, headers.Select(pair => pair.Key).OrderBy(i => i));

            Assert.Equal(3, headers.NonValidated.Count);

            headers.Clear();

            Assert.Equal(0, headers.NonValidated.Count);
            Assert.Empty(headers);
            Assert.False(headers.Contains("3"));

            Assert.True(headers.TryAddWithoutValidation("3", "newValue"));
            Assert.True(headers.TryGetValues("3", out valuesFor3));
            Assert.Equal(new[] { "newValue" }, valuesFor3);
        }

        [Fact]
        public async Task ConcurrentReads_AreThreadSafe()
        {
            if (Environment.ProcessorCount < 3) return;

            const int TestRunTimeMs = 100;

            bool running = true;
            HttpRequestHeaders headers = CreateHeaders();

            Task readerTask1 = Task.Run(ReaderWorker);
            Task readerTask2 = Task.Run(ReaderWorker);
            Task writerTask = Task.Run(() =>
            {
                while (Volatile.Read(ref running)) Volatile.Write(ref headers, CreateHeaders());
            });

            await Task.Delay(TimeSpan.FromMilliseconds(TestRunTimeMs));

            Volatile.Write(ref running, false);

            await writerTask;
            await readerTask1;
            await readerTask2;

            void ReaderWorker()
            {
                var tests = new Action<HttpRequestHeaders>[0];
                tests = new Action<HttpRequestHeaders>[]
                {
                    static headers => Assert.Equal(6, headers.NonValidated.Count),
                    static headers => Assert.True(headers.Contains("a")),
                    static headers => Assert.False(headers.Contains("b")),
                    static headers => Assert.True(headers.NonValidated.Contains("a")),
                    static headers => Assert.False(headers.NonValidated.Contains("b")),
                    static headers => Assert.True(headers.Contains("c")),
                    static headers => Assert.True(headers.Contains("f")),
                    static headers => Assert.True(headers.Contains("h")),
                    static headers => Assert.True(headers.Contains("H")),
                    static headers => Assert.True(headers.Contains("Accept")),
                    static headers => Assert.True(headers.Contains("Cache-Control")),
                    static headers => Assert.True(headers.TryGetValues("a", out var values) && values.Single() == "b"),
                    static headers => Assert.False(headers.TryGetValues("b", out _)),
                    static headers => Assert.True(headers.NonValidated.TryGetValues("a", out var values) && values.Single() == "b"),
                    static headers => Assert.False(headers.NonValidated.TryGetValues("b", out _)),
                    static headers => Assert.True(headers.TryGetValues("f", out var values) && values.Single() == "g"),
                    static headers => Assert.True(headers.NonValidated.TryGetValues("f", out var values) && values.Single() == "g"),
                    static headers => Assert.True(headers.TryGetValues("c", out var values) && values.Count() == 2 && values.First() == "d" && values.Last() == "e"),
                    static headers => Assert.True(headers.NonValidated.TryGetValues("c", out var values) && values.Count() == 2 && values.First() == "d" && values.Last() == "e"),
                    static headers => Assert.True(headers.TryGetValues("h", out var values) && values.Count() == 2 && values.First() == "i" && values.Last() == "j"),
                    static headers => Assert.True(headers.NonValidated.TryGetValues("h", out var values) && values.Count() == 2 && values.First() == "i" && values.Last() == "j"),
                    static headers => Assert.Equal("only-if-cached, private", headers.NonValidated["Cache-Control"].ToString()),
                    static headers => Assert.Equal("text/json", headers.Accept.Single().MediaType),
                    headers =>
                    {
                        var newHeaders = new HttpRequestMessage().Headers;
                        newHeaders.AddHeaders(headers);
                        for (int i = 0; i < tests.Length - 1; i++)
                        {
                            tests[i](newHeaders);
                        }
                    },
                };

                while (Volatile.Read(ref running))
                {
                    tests[Random.Shared.Next(tests.Length)](Volatile.Read(ref headers));
                }
            }

            static HttpRequestHeaders CreateHeaders()
            {
                HttpRequestHeaders headers = new HttpRequestMessage().Headers;

                var actions = new Action<HttpRequestHeaders>[]
                {
                    static headers => headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/json")),
                    static headers => headers.CacheControl = new CacheControlHeaderValue { Private = true, OnlyIfCached = true },
                    static headers => headers.Add("a", "b"),
                    static headers => headers.Add("c", new[] { "d", "e" }),
                    static headers => headers.TryAddWithoutValidation("f", "g"),
                    static headers => headers.TryAddWithoutValidation("h", new[] { "i", "j" }),
                };

                foreach (Action<HttpRequestHeaders> action in actions.OrderBy(_ => Random.Shared.Next()))
                {
                    action(headers);
                }

                return headers;
            }
        }

        [Fact]
        public void TryAddInvalidHeader_ShouldThrowFormatException()
        {
            MockHeaders headers = new MockHeaders();
            AssertExtensions.ThrowsContains<FormatException>(() => headers.Remove("\u0080"), "\u0080");
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("invalid\n")]
        [InlineData("\r")]
        [InlineData("invalid\r")]
        [InlineData("\r\n")]
        [InlineData("invalid\r\n")]
        public void TryGetValues_InvalidValuesContainingNewLines_ShouldNotRemoveInvalidValueAndShouldReturnRequestedValue(string value)
        {
            const string Name = "custom";
            HttpHeaders headers = new HttpRequestMessage().Headers;

            headers.TryAddWithoutValidation(Name, value);
            Assert.Equal(1, headers.NonValidated.Count);

            Assert.True(headers.TryGetValues(Name, out IEnumerable<string> values));

            // The entry should still exist as the parsing during the validating access should not remove the invalid value.
            Assert.Equal(1, headers.NonValidated.Count);
            Assert.Equal(1, values.Count());
            Assert.Equal(value, values.Single());
        }

        public static IEnumerable<object[]> NumberOfHeadersUpToArrayThreshold_AddNonValidated_EnumerateNonValidated()
        {
            for (int i = 0; i <= HttpHeaders.ArrayThreshold; i++)
            {
                yield return new object[] { i, false, false };
                yield return new object[] { i, false, true };
                yield return new object[] { i, true, false };
                yield return new object[] { i, true, true };
            }
        }

        public static IEnumerable<object[]> GetInvalidHeaderNames()
        {
            yield return new object[] { "invalid header" };
            yield return new object[] { "invalid\theader" };
            yield return new object[] { "invalid\rheader" };
            yield return new object[] { "invalid\nheader" };
            yield return new object[] { "invalid(header" };
            yield return new object[] { "invalid)header" };
            yield return new object[] { "invalid<header" };
            yield return new object[] { "invalid>header" };
            yield return new object[] { "invalid@header" };
            yield return new object[] { "invalid,header" };
            yield return new object[] { "invalid;header" };
            yield return new object[] { "invalid:header" };
            yield return new object[] { "invalid\\header" };
            yield return new object[] { "invalid\"header" };
            yield return new object[] { "invalid/header" };
            yield return new object[] { "invalid[header" };
            yield return new object[] { "invalid]header" };
            yield return new object[] { "invalid?header" };
            yield return new object[] { "invalid=header" };
            yield return new object[] { "invalid{header" };
            yield return new object[] { "invalid}header" };
        }

        public static IEnumerable<object[]> HeaderValuesWithNewLines()
        {
            foreach (string pattern in new[] { "*", "*foo", "* foo", "foo*", "foo* ", "foo*bar", "foo* bar" })
            foreach (string newLine in new[] { "\r", "\n", "\r\n" })
            foreach (string prefix in new[] { "", "valid, " })
            {
                yield return new object[] { prefix + pattern.Replace("*", newLine) };
            }
        }

        #region Helper methods

        private class MockHeaders : HttpHeaders
        {
            private MockHeaderParser _parser;
            private HeaderDescriptor _descriptor;

            public MockHeaderParser Parser => _parser;
            public HeaderDescriptor Descriptor => _descriptor;

            public MockHeaders(MockHeaderParser parser)
                : base()
            {
                _parser = parser;
                _descriptor = (new KnownHeader("known", HttpHeaderType.General, parser)).Descriptor;
            }

            public MockHeaders()
                : this(new MockHeaderParser())
            {
            }
        }

        private class MockHeaderParser : HttpHeaderParser
        {
            public int TryParseValueCallCount { get; set; }
            public int EmptyValueCount { get; private set; }
            public MockComparer MockComparer { get; private set; }

            public MockHeaderParser()
                : this(true)
            {
            }

            public MockHeaderParser(bool supportsMultipleValues)
                : base(supportsMultipleValues)
            {
                this.MockComparer = new MockComparer();
            }

            public MockHeaderParser(string separator)
                : base(true, separator)
            {
                this.MockComparer = new MockComparer();
            }

#region IHeaderParser Members

            public override IEqualityComparer Comparer
            {
                get { return MockComparer; }
            }

            public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
            {
                TryParseValueCallCount++;
                return TryParseValueCore(value, ref index, out parsedValue);
            }

            private bool TryParseValueCore(string value, ref int index, out object parsedValue)
            {
                parsedValue = null;

                if (value == null)
                {
                    parsedValue = null;
                    return true;
                }

                if (value == string.Empty)
                {
                    EmptyValueCount++;
                    parsedValue = null;
                    return true;
                }

                int separatorIndex = value.IndexOf(',', index);

                // Just fail if we don't support multiple values and the value is actually a list of values.
                if ((!SupportsMultipleValues) && (separatorIndex >= 0))
                {
                    return false;
                }

                if (separatorIndex == -1)
                {
                    // If the raw string just contains one value, then use the whole string.
                    separatorIndex = value.Length;
                }

                string tempValue = value.Substring(index, separatorIndex - index);

                if (tempValue.StartsWith(rawPrefix, StringComparison.Ordinal))
                {
                    index = Math.Min(separatorIndex + 1, value.Length);

                    // We "parse" the value by replacing 'rawPrefix' strings with 'parsedPrefix' string.
                    parsedValue = parsedPrefix + tempValue.Substring(rawPrefix.Length,
                        tempValue.Length - rawPrefix.Length);
                    return true;
                }

                // Only thing left is a deliberately chosen invalid value.
                Assert.StartsWith(invalidHeaderValue, tempValue, StringComparison.Ordinal);
                return false;
            }
#endregion
        }

        private class MockComparer : IEqualityComparer
        {
            public int GetHashCodeCount { get; private set; }
            public int EqualsCount { get; private set; }

#region IEqualityComparer Members

            public new bool Equals(object x, object y)
            {
                Assert.NotNull(x);
                Assert.NotNull(y);

                EqualsCount++;

                string xs = x as string;
                string ys = y as string;

                if ((xs != null) && (ys != null))
                {
                    return string.Equals(xs, ys, StringComparison.OrdinalIgnoreCase);
                }

                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                GetHashCodeCount++;
                return obj.GetHashCode();
            }
#endregion
        }

        private class CustomTypeHeaders : HttpHeaders
        {
            public CustomTypeHeaders()
            {
            }
        }

        private class CustomTypeHeaderParser : HttpHeaderParser
        {
            private static CustomTypeComparer comparer = new CustomTypeComparer();

            public override IEqualityComparer Comparer
            {
                get { return comparer; }
            }

            public CustomTypeHeaderParser()
                : base(true)
            {
            }

            public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
            {
                throw new NotImplementedException();
            }
        }

        private class CustomTypeComparer : IEqualityComparer
        {
#region IEqualityComparer Members

            public new bool Equals(object x, object y)
            {
                Assert.NotNull(x);
                Assert.NotNull(y);
                return x.Equals(y);
            }

            public int GetHashCode(object obj)
            {
                Assert.NotNull(obj);
                return obj.GetHashCode();
            }
#endregion
        }

        private class NoComparerHeaderParser : HttpHeaderParser
        {
            public NoComparerHeaderParser()
                : base(true)
            {
            }

            public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
            {
                throw new NotImplementedException();
            }
        }
#endregion
    }
}
