// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using Xunit;

namespace System.Net.Http.Tests
{
    public class HttpHeadersTest
    {
        private const string knownHeader = "known-header";
        private const string customHeader = "custom-header";
        private const string rawPrefix = "raw";
        private const string parsedPrefix = "parsed";
        private const string invalidHeaderValue = "invalid";

        [Fact]
        public void TryAddWithoutValidation_UseNullHeader_False()
        {
            MockHeaders headers = new MockHeaders();
            Assert.False(headers.TryAddWithoutValidation(null, "value"));
        }

        [Fact]
        public void TryAddWithoutValidation_UseInvalidHeader_False()
        {
            MockHeaders headers = new MockHeaders();

            // Spaces are not allowed in header names. This test is used to validate private method
            // HttpHeaders.CheckHeaders(). Since that helper method is used in all other public methods, this
            // test is not repeated for other public methods.
            Assert.False(headers.TryAddWithoutValidation("invalid header", "value"));
        }

        [Fact]
        public void TryAddWithoutValidation_AddSingleValue_ValueParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, rawPrefix);

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix, headers.First().Value.First());

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddTwoSingleValues_BothValuesParsed()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

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
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1," + rawPrefix + "2");

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
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1," + invalidHeaderValue);

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
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1," + rawPrefix + "2");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "3");

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
            headers.TryAddWithoutValidation(knownHeader, rawPrefix);
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue);

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
            // value of 'null'. HttpHeaders will remove the header from the collection since the known header doesn't
            // have a value.
            headers.TryAddWithoutValidation(knownHeader, string.Empty);
            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
            Assert.Equal(0, headers.Count());

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
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue);
            headers.TryAddWithoutValidation(knownHeader, rawPrefix);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            // If you compare this test with the previous one: Note that we reversed the order of adding the invalid
            // string and the valid string. However, when enumerating header values the order is still the same as in
            // the previous test.
            // We don't keep track of the order if we have both invalid & valid values. This would add complexity
            // and additional memory to store the information. Given how rare this scenario is we consider this
            // by design. Note that this scenario is only an issue if:
            // - The header value has an invalid format (very rare for standard headers) AND
            // - There are multiple header values (some valid, some invalid) AND
            // - The order of the headers matters (e.g. Transfer-Encoding)
            Assert.Equal(parsedPrefix, headers.First().Value.ElementAt(0));
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(1));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            string expected = knownHeader + ": " + parsedPrefix + ", " + invalidHeaderValue + "\r\n";
            Assert.Equal(expected, headers.ToString());
        }

        [Fact]
        public void TryAddWithoutValidation_AddNullValueForKnownHeader_ParserRejectsNullEmptyStringAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, (string)null);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            // MockParser is called with an empty string and decides that it is OK to have empty values but they
            // shouldn't be added to the list of header values. HttpHeaders will remove the header since it doesn't 
            // have values.
            Assert.Equal(0, headers.Count());
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddNullValueForUnknownHeader_EmptyStringAddedAsValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(customHeader, (string)null);

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
            headers.TryAddWithoutValidation(customHeader, "custom value");

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());

            Assert.Equal("custom value", headers.First().Value.First());

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddNullAndEmptyValuesToKnownHeader_HeaderRemovedFromCollection()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, (string)null);
            headers.TryAddWithoutValidation(knownHeader, string.Empty);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
            Assert.Equal(0, headers.Count());

            // TryAddWithoutValidation() adds 'null' as string.empty to distinguish between an empty raw value and no raw
            // value. When the parser is called later, the parser can decide whether empty strings are valid or not.
            // In our case the MockParser returns 'success' with a parsed value of 'null' indicating that it is OK to
            // have empty values, but they should be ignored. 
            Assert.Equal(2, headers.Parser.EmptyValueCount);
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddNullAndEmptyValuesToUnknownHeader_TwoEmptyStringsAddedAsValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(customHeader, (string)null);
            headers.TryAddWithoutValidation(customHeader, string.Empty);

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
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

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
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1," + rawPrefix + "2");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            // Since parsing the header value fails because it is composed of 2 values, the original string is added
            // to the list of 'invalid values'. Therefore we only have 1 header value (the original string).
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(rawPrefix + "1," + rawPrefix + "2", headers.First().Value.First());

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void TryAddWithoutValidation_AddValueContainingNewLine_NewLineFollowedByWhitespaceIsOKButNewLineFollowedByNonWhitespaceIsRejected()
        {
            MockHeaders headers = new MockHeaders();

            // The header parser rejects both of the following values. Both values contain new line chars. According
            // to the RFC, LWS supports newlines followed by whitespaces. I.e. the first value gets rejected by the
            // parser, but added to the list of invalid values.
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\n other: value"); // OK, LWS is allowed
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(invalidHeaderValue + "\r\n other: value", headers.First().Value.First());
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // This value is considered invalid (newline char followed by non-whitepace). However, since
            // TryAddWithoutValidation() only causes the header value to be analyzed when it gets actually accessed, no
            // exception is thrown. Instead the value is discarded and a warning is logged.
            headers.Clear();
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\nother:value");
            Assert.False(headers.Contains(knownHeader));
            Assert.Equal(0, headers.Count());

            // Adding newline followed by whitespace to a custom header is OK.
            headers.Clear();
            headers.TryAddWithoutValidation("custom", "value\r\n other: value"); // OK, LWS is allowed
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal("value\r\n other: value", headers.First().Value.First());

            // Adding newline followed by non-whitespace chars is invalid. The value is discarded and a warning is
            // logged.
            headers.Clear();
            headers.TryAddWithoutValidation("custom", "value\r\nother: value");
            Assert.False(headers.Contains("custom"));
            Assert.Equal(0, headers.Count());

            // Also ending a value with newline is invalid. Verify that valid values are added.
            headers.Clear();
            headers.Parser.TryParseValueCallCount = 0;
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "\rvalid");
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\n");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "\n," + invalidHeaderValue + "\r\nother");
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "\rvalid", headers.First().Value.First());
            Assert.Equal(4, headers.Parser.TryParseValueCallCount);

            headers.Clear();
            headers.TryAddWithoutValidation("custom", "value\r\ninvalid");
            headers.TryAddWithoutValidation("custom", "value\r\n valid");
            headers.TryAddWithoutValidation("custom", "validvalue, invalid\r\nvalue");
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal("value\r\n valid", headers.First().Value.First());
        }

        [Fact]
        public void TryAddWithoutValidation_MultipleAddInvalidValuesToNonExistingHeader_AddHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, new string[] { invalidHeaderValue });

            // Make sure the header did not get added since we just tried to add an invalid value.
            Assert.True(headers.Contains(knownHeader));
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void TryAddWithoutValidation_MultipleAddValidValueThenAddInvalidValuesToExistingHeader_AddValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, new string[] { rawPrefix + "2", invalidHeaderValue });

            Assert.True(headers.Contains(knownHeader));
            Assert.Equal(3, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(2));
        }

        [Fact]
        public void TryAddWithoutValidation_MultipleAddValidValueThenAddInvalidValuesToNonExistingHeader_AddHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, new string[] { rawPrefix + "1", invalidHeaderValue });

            Assert.True(headers.Contains(knownHeader));
            Assert.Equal(2, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(1));
        }

        [Fact]
        public void TryAddWithoutValidation_MultipleAddNullValueCollection_Throws()
        {
            MockHeaders headers = new MockHeaders();
            string[] values = null;
            
            Assert.Throws<ArgumentNullException>(() => { headers.TryAddWithoutValidation(knownHeader, values); });
        }

        [Fact]
        public void Add_SingleUseNullHeaderName_Throw()
        {
            MockHeaders headers = new MockHeaders();
            
            Assert.Throws<ArgumentException>(() => { headers.Add(null, "value"); });
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
            headers.Add(knownHeader, rawPrefix);

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
            headers.Add(knownHeader, (string)null);
            headers.Add(knownHeader, string.Empty);
            headers.Add(knownHeader, string.Empty);

            // Add() should trigger parsing.
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);

            Assert.Equal(0, headers.Count());
        }

        [Fact]
        public void Add_SingleAddInvalidValueToNonExistingHeader_ThrowAndDontAddHeader()
        {
            // Since Add() immediately parses the value, it will throw an exception if the value is invalid.
            MockHeaders headers = new MockHeaders();
            Assert.Throws<FormatException>(() => { headers.Add(knownHeader, invalidHeaderValue); });

            // Make sure the header did not get added to the store.
            Assert.False(headers.Contains(knownHeader),
                "No header expected to be added since header value was invalid.");
        }

        [Fact]
        public void Add_SingleAddValidValueThenAddInvalidValue_ThrowAndHeaderContainsValidValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix);

            Assert.Throws<FormatException>(() => { headers.Add(knownHeader, invalidHeaderValue); });

            // Make sure the header did not get removed due to the failed add.
            Assert.True(headers.Contains(knownHeader), "Header was removed even if there is a valid header value.");
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix, headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void Add_MultipleAddInvalidValuesToNonExistingHeader_ThrowAndDontAddHeader()
        {
            MockHeaders headers = new MockHeaders();

            Assert.Throws<FormatException>(() => { headers.Add(knownHeader, new string[] { invalidHeaderValue }); });

            // Make sure the header did not get added since we just tried to add an invalid value.
            Assert.False(headers.Contains(knownHeader), "Header was added even if we just added an invalid value.");
        }

        [Fact]
        public void Add_MultipleAddValidValueThenAddInvalidValuesToExistingHeader_ThrowAndDontAddHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");

            Assert.Throws<FormatException>(() => { headers.Add(knownHeader, new string[] { rawPrefix + "2", invalidHeaderValue }); });

            // Make sure the header did not get removed due to the failed add. Note that the first value in the array
            // is valid, so it gets added. I.e. we have 2 values.
            Assert.True(headers.Contains(knownHeader), "Header was removed even if there is a valid header value.");
            Assert.Equal(2, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
        }

        [Fact]
        public void Add_MultipleAddValidValueThenAddInvalidValuesToNonExistingHeader_ThrowAndDontAddHeader()
        {
            MockHeaders headers = new MockHeaders();

            Assert.Throws<FormatException>(() => { headers.Add(knownHeader, new string[] { rawPrefix + "1", invalidHeaderValue }); });

            // Make sure the header got added due to the valid add. Note that the first value in the array
            // is valid, so it gets added.
            Assert.True(headers.Contains(knownHeader), "Header was not added even though we added 1 valid value.");
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void Add_SingleAddThreeValidValues_ValuesParsedCorrectly()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");
            headers.Add(knownHeader, rawPrefix + "2");
            headers.Add(knownHeader, rawPrefix + "3");

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

            try
            {
                headers.Add(knownHeader, rawPrefix + "1");
            }
            catch (Exception e)
            {
                Assert.True(false, string.Format("Adding the first header already threw exception: {0}", e));
            }

            Assert.Throws<FormatException>(() => { headers.Add(knownHeader, rawPrefix + "2"); });

            // Verify that the first header value is still there.
            Assert.Equal(1, headers.First().Value.Count());
        }

        [Fact]
        public void Add_SingleFirstTryAddWithoutValidationForValidValueThenAdd_TwoParsedValuesAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1");
            headers.Add(knownHeader, rawPrefix + "2");

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
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue);
            headers.Add(knownHeader, rawPrefix + "1");

            // Add() should trigger parsing. TryAddWithoutValidation() doesn't trigger parsing, but Add() triggers
            // parsing of raw header values (TryParseValue() is called)
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(2, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(invalidHeaderValue, headers.First().Value.ElementAt(1));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleFirstTryAddWithoutValidationForEmptyValueThenAdd_OneParsedValueAddedEmptyIgnored()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, string.Empty);
            headers.Add(knownHeader, rawPrefix + "1");

            // Add() should trigger parsing. TryAddWithoutValidation() doesn't trigger parsing, but Add() triggers
            // parsing of raw header values (TryParseValue() is called)
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());

            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));

            // Value is already parsed. There shouldn't be additional calls to the parser.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Add_SingleFirstAddThenTryAddWithoutValidation_TwoParsedValuesAdded()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

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
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");
            headers.Add(knownHeader, rawPrefix + "3");

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

            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1");
            Assert.Throws<FormatException>(() => {headers.Add(knownHeader, rawPrefix + "2"); });
        }

        [Fact]
        public void Add_SingleFirstAddThenTryAddWithoutValidationToSingleValueHeader_BothParsedAndInvalidValue()
        {
            MockHeaderParser parser = new MockHeaderParser(false); // doesn't support multiple values.
            MockHeaders headers = new MockHeaders(parser);
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

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
            headers.Add(knownHeader, values);

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
            headers.Add(knownHeader, rawPrefix + "1," + rawPrefix + "2," + rawPrefix + "3");

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
            
            Assert.Throws<ArgumentNullException>(() => { headers.Add(knownHeader, values); });
        }

        [Fact]
        public void Add_SingleAddCustomHeaderWithNullValue_HeaderIsAddedWithEmptyStringValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(customHeader, (string)null);

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

        [Fact]
        public void Add_AddValueContainingNewLine_NewLineFollowedByWhitespaceIsOKButNewLineFollowedByNonWhitespaceIsRejected()
        {
            MockHeaders headers = new MockHeaders();

            headers.Clear();
            headers.Add("custom", "value\r");
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal("value\r", headers.First().Value.First());

            headers.Clear();
            Assert.Throws<FormatException>(() => { headers.Add("custom", new string[] { "valid\n", "invalid\r\nother" }); });
            Assert.Equal(1, headers.Count());
            Assert.Equal(1, headers.First().Value.Count());
            Assert.Equal("valid\n", headers.First().Value.First());
        }

        [Fact]
        public void RemoveParsedValue_AddValueAndRemoveIt_NoHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");

            // Remove the parsed value (note the original string 'raw1' was "parsed" to 'parsed1')
            Assert.True(headers.RemoveParsedValue(knownHeader, parsedPrefix + "1"));

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(0, headers.Count());

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Remove the value again: It shouldn't be found in the store.
            Assert.False(headers.RemoveParsedValue(knownHeader, parsedPrefix + "1"));
        }

        [Fact]
        public void RemoveParsedValue_AddInvalidValueAndRemoveValidValue_InvalidValueRemains()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue);

            // Remove a valid value which is not in the store.
            Assert.False(headers.RemoveParsedValue(knownHeader, parsedPrefix));

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(1, headers.Count());
            Assert.Equal(invalidHeaderValue, headers.GetValues(knownHeader).First());

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // Remove the value again: It shouldn't be found in the store.
            Assert.False(headers.RemoveParsedValue(knownHeader, parsedPrefix + "1"));
        }

        [Fact]
        public void RemoveParsedValue_ParserWithNoEqualityComparer_CaseSensitiveComparison()
        {
            CustomTypeHeaders headers = new CustomTypeHeaders("noComparerHeader", new NoComparerHeaderParser());
            headers.AddParsedValue("noComparerHeader", "lowercasevalue");

            // Since we don't provide a comparer, the default string.Equals() is called which is case-sensitive. So
            // the following call should return false.
            Assert.False(headers.RemoveParsedValue("noComparerHeader", "LOWERCASEVALUE"));

            // Now we try to remove the value using the correct casing. This should work.
            Assert.True(headers.RemoveParsedValue("noComparerHeader", "lowercasevalue"));

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(0, headers.Count());
        }

        [Fact]
        public void RemoveParsedValue_AddTwoValuesAndRemoveThem_NoHeader()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");
            headers.Add(knownHeader, rawPrefix + "2");

            // Remove the parsed value (note the original string 'raw1' was "parsed" to 'parsed1')
            Assert.True(headers.RemoveParsedValue(knownHeader, parsedPrefix + "1"));
            Assert.True(headers.RemoveParsedValue(knownHeader, parsedPrefix + "2"));

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            // Note that when the last value of a header gets removed, the whole header gets removed.
            Assert.Equal(0, headers.Count());

            Assert.Equal(2, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void RemoveParsedValue_AddTwoValuesAndRemoveFirstOne_SecondValueRemains()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");
            headers.Add(knownHeader, rawPrefix + "2");

            // Remove the parsed value (note the original string 'raw1' was "parsed" to 'parsed1')
            Assert.True(headers.RemoveParsedValue(knownHeader, parsedPrefix + "1"));

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
            headers.Add(knownHeader, rawPrefix + "1");
            headers.Add(knownHeader, rawPrefix + "2");

            // Remove the parsed value (note the original string 'raw2' was "parsed" to 'parsed2')
            Assert.True(headers.RemoveParsedValue(knownHeader, parsedPrefix + "2"));

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
            headers.Add(knownHeader, rawPrefix);

            // Header 'non-existing-header' can't be found, so false is returned.
            Assert.False(headers.RemoveParsedValue("non-existing-header", "doesntexist"));
        }

        [Fact]
        public void RemoveParsedValue_RemoveFromUninitializedHeaderStore_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();

            // If we never add a header value, the whole header (and also the header store) doesn't exist.
            // Make sure we considered this case.
            Assert.False(headers.RemoveParsedValue(knownHeader, "doesntexist"));
        }

        [Fact]
        public void RemoveParsedValue_AddOneValueToKnownHeaderAndCompareWithValueThatDiffersInCase_CustomComparerUsedForComparison()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(knownHeader, "value");

            // Our custom comparer (MockComparer) does case-insensitive value comparison. Verify that our custom
            // comparer is used to compare the header value.
            Assert.True(headers.RemoveParsedValue(knownHeader, "VALUE"));
            Assert.False(headers.Contains(knownHeader), "Header should be removed after removing value.");
            Assert.Equal(1, headers.Parser.MockComparer.EqualsCount);
        }

        [Fact]
        public void RemoveParsedValue_AddTwoValuesToKnownHeaderAndCompareWithValueThatDiffersInCase_CustomComparerUsedForComparison()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(knownHeader, "differentvalue");
            headers.AddParsedValue(knownHeader, "value");

            // Our custom comparer (MockComparer) does case-insensitive value comparison. Verify that our custom
            // comparer is used to compare the header value.
            // Note that since we added 2 values a different code path than in the previous test is used. In this
            // case we have stored the values as List<string> internally.
            Assert.True(headers.RemoveParsedValue(knownHeader, "VALUE"));
            Assert.Equal(1, headers.GetValues(knownHeader).Count());
            Assert.Equal(2, headers.Parser.MockComparer.EqualsCount);
        }

        [Fact]
        public void RemoveParsedValue_FirstAddInvalidNewlineCharsValueThenCallRemoveParsedValue_HeaderRemoved()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with invalid newline chars.
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\ninvalid");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            headers.RemoveParsedValue(knownHeader, "");

            Assert.False(headers.Contains(knownHeader), "Store should not have an entry for 'knownHeader'.");
        }

        [Fact]
        public void RemoveParsedValue_FirstAddInvalidNewlineCharsValueThenAddValidValueThenCallAddParsedValue_HeaderRemoved()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with invalid newline chars.
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\ninvalid");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            headers.RemoveParsedValue(knownHeader, parsedPrefix + "1");

            Assert.False(headers.Contains(knownHeader), "Store should not have an entry for 'knownHeader'.");
        }

        [Fact]
        public void Clear_AddMultipleHeadersAndThenClear_NoHeadersInCollection()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");
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

        [Fact]
        public void Remove_UseNullHeader_Throw()
        {
            MockHeaders headers = new MockHeaders();
            
            Assert.Throws<ArgumentException>(() => { headers.Remove(null); });
        }

        [Fact]
        public void Remove_AddMultipleHeadersAndDeleteFirstAndLast_FirstAndLastHeaderRemoved()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");
            headers.Add("custom1", "customValue1");
            headers.Add("custom2", "customValue2");
            headers.Add("lastheader", "customValue3");

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            // We added 4 different headers
            Assert.Equal(4, headers.Count());

            // Remove first header
            Assert.True(headers.Remove(knownHeader));
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
            Assert.False(headers.Remove(knownHeader));
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

        [Fact]
        public void TryGetValues_UseNullHeader_False()
        {
            MockHeaders headers = new MockHeaders();

            IEnumerable<string> values = null;

            Assert.False(headers.TryGetValues(null, out values));
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
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");
            headers.TryAddWithoutValidation(knownHeader, string.Empty);

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            IEnumerable<string> values = null;

            Assert.True(headers.TryGetValues(knownHeader, out values));
            Assert.NotNull(values);

            // TryGetValues() should trigger parsing of values added with TryAddWithoutValidation()
            Assert.Equal(3, headers.Parser.TryParseValueCallCount);

            Assert.Equal(2, values.Count());

            // Check returned values
            Assert.Equal(parsedPrefix + "1", values.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", values.ElementAt(1));
        }

        [Fact]
        public void GetValues_UseNullHeader_Throw()
        {
            MockHeaders headers = new MockHeaders();
            
            Assert.Throws<ArgumentException>(() => { headers.GetValues(null); });
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
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            IEnumerable<string> values = headers.GetValues(knownHeader);
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
            headers.TryAddWithoutValidation(customHeader, (string)null);
            headers.Add(knownHeader, string.Empty);

            // In the known header case, the MockParser accepts empty values but tells the store to not add the value.
            // Since no value is added for 'knownHeader', HttpHeaders removes the header from the store. This is only
            // done for known headers. Custom headers are allowed to have empty/null values as shown by 
            // 'valuesForCustomHeaders' below
            Assert.False(headers.Contains(knownHeader));

            // In the custom header case, we add whatever the users adds (besides that we add string.Empty if the
            // user adds null). So here we do have 1 value: string.Empty.
            IEnumerable<string> valuesForCustomHeader = headers.GetValues(customHeader);
            Assert.NotNull(valuesForCustomHeader);
            Assert.Equal(1, valuesForCustomHeader.Count());
            Assert.Equal(string.Empty, valuesForCustomHeader.First());
        }

        [Fact]
        public void GetParsedValues_GetValuesFromUninitializedHeaderStore_ReturnsNull()
        {
            MockHeaders headers = new MockHeaders();

            // Get header values from uninitialized store (store collection is null).
            object storeValue = headers.GetParsedValues("doesntexist");
            Assert.Null(storeValue);
        }

        [Fact]
        public void GetParsedValues_GetValuesForNonExistingHeader_ReturnsNull()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");

            // Get header values for non-existing header (but other headers exist in the store).
            object storeValue = headers.GetParsedValues("doesntexist");
            Assert.Null(storeValue);
        }

        [Fact]
        public void GetParsedValues_GetSingleValueForExistingHeader_ReturnsAddedValue()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");

            // Get header values for non-existing header (but other headers exist in the store).
            object storeValue = headers.GetParsedValues("custom1");
            Assert.NotNull(storeValue);

            // If we only have one value, then GetValues() should return just the value and not wrap it in a List<T>.
            Assert.Equal("customValue1", storeValue);
        }

        [Fact]
        public void GetParsedValues_HeaderWithEmptyValues_ReturnsEmpty()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, string.Empty);

            object storeValue = headers.GetParsedValues(knownHeader);
            Assert.Null(storeValue);
        }

        [Fact]
        public void GetParsedValues_GetMultipleValuesForExistingHeader_ReturnsListOfValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation("custom", rawPrefix + "0"); // this must not influence the result.
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            object storeValue = headers.GetParsedValues(knownHeader);
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
        public void GetParsedValues_GetValuesForExistingHeaderWithInvalidValues_ReturnsOnlyParsedValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix);

            // Here we add an invalid value. GetValues<T> only returns parsable values. So this value should get
            // parsed, however it will be added to the 'invalid values' list and thus is not part of the collection
            // returned by the enumerator.
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue);

            // Only 1 value should get parsed (call to Add() with known header value).
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            object storeValue = headers.GetParsedValues(knownHeader);
            Assert.NotNull(storeValue);

            // GetValues<T>() should trigger parsing of values added with TryAddWithoutValidation()
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            // Since we added only one valid value to 'knownHeader', we expect GetValues() to return a that value.
            Assert.Equal(parsedPrefix, storeValue);
        }

        [Fact]
        public void GetParsedValues_GetValuesForExistingHeaderWithOnlyInvalidValues_ReturnsEmptyEnumerator()
        {
            MockHeaders headers = new MockHeaders();

            // Here we add an invalid value. GetValues<T> only returns parsable values. So this value should get
            // parsed, however it will be added to the 'invalid values' list and thus is not part of the collection
            // returned by the enumerator.
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            object storeValue = headers.GetParsedValues(knownHeader);
            Assert.Null(storeValue);

            // GetValues<T>() should trigger parsing of values added with TryAddWithoutValidation()
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void GetParsedValues_AddInvalidValueToHeader_HeaderGetsRemovedAndNullReturned()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\ninvalid");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            object storeValue = headers.GetParsedValues(knownHeader);
            Assert.Null(storeValue);
            Assert.False(headers.Contains(knownHeader));
        }

        [Fact]
        public void GetParsedValues_GetParsedValuesForKnownHeaderWithInvalidNewlineChars_ReturnsNull()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with invalid newline chars.
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\ninvalid");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
            Assert.Null(headers.GetParsedValues(knownHeader));
            Assert.Equal(0, headers.Count());
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void GetHeaderStrings_SetValidAndInvalidHeaderValues_AllHeaderValuesReturned()
        {
            MockHeaderParser parser = new MockHeaderParser("---");
            MockHeaders headers = new MockHeaders(parser);

            // Add header value with invalid newline chars.
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, "value2,value3");
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue);

            foreach (var header in headers.GetHeaderStrings())
            {
                Assert.Equal(knownHeader, header.Key);
                // Note that raw values don't get parsed but just added to the result.
                Assert.Equal("value2,value3---" + invalidHeaderValue + "---" + parsedPrefix + "1", header.Value);
            }
        }

        [Fact]
        public void GetHeaderStrings_SetMultipleHeaders_AllHeaderValuesReturned()
        {
            MockHeaderParser parser = new MockHeaderParser(true);
            MockHeaders headers = new MockHeaders(parser);

            // Add header value with invalid newline chars.
            headers.Add(knownHeader, rawPrefix + "1");
            headers.Add("header2", "value2");
            headers.Add("header3", (string)null);
            headers.Add("header4", "value41");
            headers.Add("header4", "value42");

            string[] expectedHeaderNames = { knownHeader, "header2", "header3", "header4" };
            string[] expectedHeaderValues = { parsedPrefix + "1", "value2", "", "value41, value42" };
            int i = 0;

            foreach (var header in headers.GetHeaderStrings())
            {
                Assert.NotEqual(expectedHeaderNames.Length, i);
                Assert.Equal(expectedHeaderNames[i], header.Key);
                Assert.Equal(expectedHeaderValues[i], header.Value);
                i++;
            }
        }

        [Fact]
        public void GetHeaderStrings_SetMultipleValuesOnSingleValueHeader_AllHeaderValuesReturned()
        {
            MockHeaderParser parser = new MockHeaderParser(false);
            MockHeaders headers = new MockHeaders(parser);

            headers.TryAddWithoutValidation(knownHeader, "value1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix);

            foreach (var header in headers.GetHeaderStrings())
            {
                Assert.Equal(knownHeader, header.Key);
                // Note that the added rawPrefix did not get parsed
                Assert.Equal("value1, " + rawPrefix, header.Value);
            }
        }

        [Fact]
        public void Contains_UseNullHeader_Throw()
        {
            MockHeaders headers = new MockHeaders();
            
            Assert.Throws<ArgumentException>(() => { headers.Contains(null); });
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
            headers.Add(knownHeader, rawPrefix);
            Assert.False(headers.Contains("doesntexist"));
        }

        [Fact]
        public void Contains_CallContainsForEmptyHeader_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, string.Empty);
            Assert.False(headers.Contains(knownHeader));
        }

        [Fact]
        public void Contains_CallContainsForExistingHeader_ReturnsTrue()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add("custom1", "customValue1");
            headers.Add("custom2", "customValue2");
            headers.Add("custom3", "customValue3");
            headers.Add("custom4", "customValue4");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix);

            // Nothing got parsed so far since we just added custom headers and for the known header we called
            // TryAddWithoutValidation().
            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.True(headers.Contains(knownHeader));

            // Contains() should trigger parsing of values added with TryAddWithoutValidation(): If the value was invalid,
            // i.e. contains invalid newline chars, then the header will be removed from the collection.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void Contains_AddValuesWithInvalidNewlineChars_HeadersGetRemovedWhenCallingContains()
        {
            MockHeaders headers = new MockHeaders();

            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\ninvalid");
            headers.TryAddWithoutValidation("custom", "invalid\r\nvalue");

            Assert.False(headers.Contains(knownHeader), "Store should not have an entry for 'knownHeader'.");
            Assert.False(headers.Contains("custom"), "Store should not have an entry for 'custom'.");
        }

        [Fact]
        public void GetEnumerator_GetEnumeratorFromUninitializedHeaderStore_ReturnsEmptyEnumerator()
        {
            MockHeaders headers = new MockHeaders();

            var enumerator = headers.GetEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void GetEnumerator_FirstHeaderWithOneValueSecondHeaderWithTwoValues_EnumeratorReturnsTwoHeaders()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(customHeader, "custom0");
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

            // The value added with TryAddWithoutValidation() wasn't parsed yet.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            var enumerator = headers.GetEnumerator();

            // Getting the enumerator doesn't trigger parsing.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            Assert.True(enumerator.MoveNext());
            Assert.Equal(customHeader, enumerator.Current.Key);
            Assert.Equal(1, enumerator.Current.Value.Count());
            Assert.Equal("custom0", enumerator.Current.Value.ElementAt(0));

            // Starting using the enumerator will trigger parsing of raw values. The first header is not a known
            // header, so there shouldn't be any parsing.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            Assert.True(enumerator.MoveNext());
            Assert.Equal(knownHeader, enumerator.Current.Key);
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
            headers.Add(customHeader, string.Empty);
            headers.Add(knownHeader, string.Empty);

            var enumerator = headers.GetEnumerator();

            Assert.True(enumerator.MoveNext());
            Assert.Equal(customHeader, enumerator.Current.Key);
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

            var enumerator = headersAsIEnumerable.GetEnumerator();

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
        public void AddParsedValue_AddSingleValueToNonExistingHeader_HeaderGetsCreatedAndValueAdded()
        {
            Uri headerValue = new Uri("http://example.org/");

            CustomTypeHeaders headers = new CustomTypeHeaders("myheader", new CustomTypeHeaderParser());
            headers.AddParsedValue("myheader", headerValue);

            Assert.True(headers.Contains("myheader"), "Store doesn't have the header after adding a value to it.");

            Assert.Equal(headerValue.ToString(), headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void AddParsedValue_AddValueTypeValueToNonExistingHeader_HeaderGetsCreatedAndBoxedValueAdded()
        {
            int headerValue = 5;

            CustomTypeHeaders headers = new CustomTypeHeaders("myheader", new CustomTypeHeaderParser());
            headers.AddParsedValue("myheader", headerValue);

            Assert.True(headers.Contains("myheader"), "Store doesn't have the header after adding a value to it.");

            Assert.Equal(headerValue.ToString(), headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void AddParsedValue_AddTwoValuesToNonExistingHeader_HeaderGetsCreatedAndValuesAdded()
        {
            Uri headerValue1 = new Uri("http://example.org/1/");
            Uri headerValue2 = new Uri("http://example.org/2/");

            CustomTypeHeaders headers = new CustomTypeHeaders("myheader", new CustomTypeHeaderParser());
            headers.AddParsedValue("myheader", headerValue1);

            // Adding a second value will cause a List<T> to be created in order to store values. If we just add
            // one value, no List<T> is created, but the header is just added as store value.
            headers.AddParsedValue("myheader", headerValue2);

            Assert.True(headers.Contains("myheader"), "Store doesn't have the header after adding a value to it.");
            Assert.Equal(2, headers.GetValues("myheader").Count());

            Assert.Equal(headerValue1.ToString(), headers.First().Value.ElementAt(0));
            Assert.Equal(headerValue2.ToString(), headers.First().Value.ElementAt(1));
        }

        [Fact]
        public void AddParsedValue_UseDifferentAddMethods_AllValuesAddedCorrectly()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            headers.AddParsedValue(knownHeader, parsedPrefix + "3");

            // Adding a parsed value, will trigger all raw values to be parsed.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(3, headers.GetValues(knownHeader).Count());
            Assert.Equal(parsedPrefix + "1", headers.First().Value.ElementAt(0));
            Assert.Equal(parsedPrefix + "2", headers.First().Value.ElementAt(1));
            Assert.Equal(parsedPrefix + "3", headers.First().Value.ElementAt(2));
        }

        [Fact]
        public void AddParsedValue_FirstAddInvalidNewlineCharsValueThenCallAddParsedValue_ParsedValueAdded()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with invalid newline chars.
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\ninvalid");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            headers.AddParsedValue(knownHeader, parsedPrefix + "1");

            Assert.True(headers.Contains(knownHeader), "Store should have an entry for 'knownHeader'.");
            Assert.Equal(1, headers.GetValues(knownHeader).Count());
            Assert.Equal(parsedPrefix + "1", headers.GetValues(knownHeader).First());
        }

        [Fact]
        public void AddParsedValue_FirstAddInvalidNewlineCharsValueThenAddValidValueThenCallAddParsedValue_ParsedValueAdded()
        {
            MockHeaders headers = new MockHeaders();

            // Add header value with invalid newline chars.
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue + "\r\ninvalid");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "0");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            headers.AddParsedValue(knownHeader, parsedPrefix + "1");

            Assert.True(headers.Contains(knownHeader), "Store should have an entry for 'knownHeader'.");
            Assert.Equal(2, headers.GetValues(knownHeader).Count());
            Assert.Equal(parsedPrefix + "0", headers.GetValues(knownHeader).ElementAt(0));
            Assert.Equal(parsedPrefix + "1", headers.GetValues(knownHeader).ElementAt(1));
        }

        [Fact]
        public void SetParsedValue_AddSingleValueToNonExistingHeader_HeaderGetsCreatedAndValueAdded()
        {
            Uri headerValue = new Uri("http://example.org/");

            CustomTypeHeaders headers = new CustomTypeHeaders("myheader", new CustomTypeHeaderParser());
            headers.SetParsedValue("myheader", headerValue);

            Assert.True(headers.Contains("myheader"), "Store doesn't have the header after adding a value to it.");

            Assert.Equal(headerValue.ToString(), headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void SetParsedValue_SetTwoValuesToNonExistingHeader_HeaderGetsCreatedAndLastValueAdded()
        {
            Uri headerValue1 = new Uri("http://example.org/1/");
            Uri headerValue2 = new Uri("http://example.org/2/");

            CustomTypeHeaders headers = new CustomTypeHeaders("myheader", new CustomTypeHeaderParser());
            headers.SetParsedValue("myheader", headerValue1);

            // The following line will remove the previously added values and replace them with the provided value.
            headers.SetParsedValue("myheader", headerValue2);

            Assert.True(headers.Contains("myheader"), "Store doesn't have the header after adding a value to it.");
            Assert.Equal(1, headers.GetValues("myheader").Count());

            // The second value replaces the first value.
            Assert.Equal(headerValue2.ToString(), headers.First().Value.ElementAt(0));
        }

        [Fact]
        public void SetParsedValue_SetValueAfterAddingMultipleValues_SetValueReplacesOtherValues()
        {
            MockHeaders headers = new MockHeaders();
            headers.Add(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");

            Assert.Equal(1, headers.Parser.TryParseValueCallCount);

            headers.SetParsedValue(knownHeader, parsedPrefix + "3");

            // Adding a parsed value, will trigger all raw values to be parsed.
            Assert.Equal(2, headers.Parser.TryParseValueCallCount);

            Assert.Equal(1, headers.GetValues(knownHeader).Count());
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
            headers.TryAddWithoutValidation(knownHeader, rawPrefix);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.False(headers.ContainsParsedValue(customHeader, "custom1"));

            // ContainsParsedValue() must not trigger raw value parsing for headers other than the requested one.
            // In this case we expect ContainsParsedValue(customeHeader) not to trigger raw value parsing for
            // 'knownHeader'.
            Assert.Equal(0, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void ContainsParsedValue_ContainsParsedValueForNonExistingHeaderValue_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(knownHeader, "value1");
            headers.AddParsedValue(knownHeader, "value2");

            // After adding two values to header 'knownHeader' we ask for a non-existing value.
            Assert.False(headers.ContainsParsedValue(knownHeader, "doesntexist"));
        }

        [Fact]
        public void ContainsParsedValue_ContainsParsedValueForExistingHeaderButNonAvailableValue_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, rawPrefix);

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.False(headers.ContainsParsedValue(knownHeader, "custom1"));

            // ContainsParsedValue() must trigger raw value parsing for the header it was asked for.
            Assert.Equal(1, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void ContainsParsedValue_ContainsParsedValueForExistingHeaderWithAvailableValue_ReturnsTrue()
        {
            MockHeaders headers = new MockHeaders();
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "1");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "2");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "3");
            headers.TryAddWithoutValidation(knownHeader, rawPrefix + "4");

            Assert.Equal(0, headers.Parser.TryParseValueCallCount);

            Assert.True(headers.ContainsParsedValue(knownHeader, parsedPrefix + "3"));

            // ContainsParsedValue() must trigger raw value parsing for the header it was asked for.
            Assert.Equal(4, headers.Parser.TryParseValueCallCount);
        }

        [Fact]
        public void ContainsParsedValue_AddOneValueToKnownHeaderAndCompareWithValueThatDiffersInCase_CustomComparerUsedForComparison()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(knownHeader, "value");

            // Our custom comparer (MockComparer) does case-insensitive value comparison. Verify that our custom
            // comparer is used to compare the header value.
            Assert.True(headers.ContainsParsedValue(knownHeader, "VALUE"));
            Assert.Equal(1, headers.Parser.MockComparer.EqualsCount);

            headers.Clear();
            headers.TryAddWithoutValidation(knownHeader, invalidHeaderValue);
            Assert.False(headers.ContainsParsedValue(knownHeader, invalidHeaderValue));
        }

        [Fact]
        public void ContainsParsedValue_AddTwoValuesToKnownHeaderAndCompareWithValueThatDiffersInCase_CustomComparerUsedForComparison()
        {
            MockHeaders headers = new MockHeaders();
            headers.AddParsedValue(knownHeader, "differentvalue");
            headers.AddParsedValue(knownHeader, "value");

            // Our custom comparer (MockComparer) does case-insensitive value comparison. Verify that our custom
            // comparer is used to compare the header value.
            // Note that since we added 2 values a different code path than in the previous test is used. In this
            // case we have stored the values as List<string> internally.
            Assert.True(headers.ContainsParsedValue(knownHeader, "VALUE"));
            Assert.Equal(2, headers.Parser.MockComparer.EqualsCount);
        }

        [Fact]
        public void ContainsParsedValue_ParserWithNoEqualityComparer_CaseSensitiveComparison()
        {
            CustomTypeHeaders headers = new CustomTypeHeaders("noComparerHeader", new NoComparerHeaderParser());
            headers.AddParsedValue("noComparerHeader", "lowercasevalue");

            // Since we don't provide a comparer, the default string.Equals() is called which is case-sensitive. So
            // the following call should return false.
            Assert.False(headers.ContainsParsedValue("noComparerHeader", "LOWERCASEVALUE"));

            // Now we try to use the correct casing. This should return true.
            Assert.True(headers.ContainsParsedValue("noComparerHeader", "lowercasevalue"));
        }

        [Fact]
        public void ContainsParsedValue_CallFromEmptyHeaderStore_ReturnsFalse()
        {
            MockHeaders headers = new MockHeaders();

            // This will create a header entry with no value.
            headers.Add(knownHeader, string.Empty);

            Assert.False(headers.Contains(knownHeader), "Expected known header to be in the store.");

            // This will just return fals and not touch the header.
            Assert.False(headers.ContainsParsedValue(knownHeader, "x"),
                "Expected 'ContainsParsedValue' to return false.");
        }

        [Fact]
        public void AddHeaders_SourceAndDestinationStoreHaveMultipleHeaders_OnlyHeadersNotInDestinationAreCopiedFromSource()
        {
            Dictionary<string, HttpHeaderParser> parserStore = new Dictionary<string, HttpHeaderParser>();
            parserStore.Add("known1", new MockHeaderParser());
            parserStore.Add("known2", new MockHeaderParser());
            parserStore.Add("known3", new MockHeaderParser());
            parserStore.Add("known4", new CustomTypeHeaderParser());

            // Add header values to the source store.
            MockHeaders source = new MockHeaders(parserStore);
            source.Add("custom1", "source10");
            source.Add("custom1", "source11");

            source.TryAddWithoutValidation("custom2", "source2");

            source.Add("known1", rawPrefix + "3");
            source.TryAddWithoutValidation("known1", rawPrefix + "4");

            source.TryAddWithoutValidation("known2", rawPrefix + "5");
            source.TryAddWithoutValidation("known2", invalidHeaderValue);
            source.TryAddWithoutValidation("known2", rawPrefix + "7");

            // this header value gets removed when it gets parsed.
            source.TryAddWithoutValidation("known3", (string)null);
            source.Add("known3", string.Empty);

            DateTimeOffset known4Value1 = new DateTimeOffset(2010, 6, 15, 18, 31, 34, TimeSpan.Zero);
            DateTimeOffset known4Value2 = new DateTimeOffset(2010, 4, 8, 11, 21, 04, TimeSpan.Zero);
            source.AddParsedValue("known4", known4Value1);
            source.AddParsedValue("known4", known4Value2);

            source.Add("custom5", "source5");
            source.TryAddWithoutValidation("custom6", (string)null);

            // This header value gets added even though it doesn't have values. But since this is a custom header we
            // assume it supports empty values.
            source.TryAddWithoutValidation("custom7", (string)null);
            source.Add("custom7", string.Empty);

            // Add header values to the destination store.
            MockHeaders destination = new MockHeaders(parserStore);
            destination.Add("custom2", "destination1");
            destination.Add("known1", rawPrefix + "9");

            // Now add all headers that are in source but not destination to destination.
            destination.AddHeaders(source);

            Assert.Equal(8, destination.Count());

            Assert.Equal(2, destination.GetValues("custom1").Count());
            Assert.Equal("source10", destination.GetValues("custom1").ElementAt(0));
            Assert.Equal("source11", destination.GetValues("custom1").ElementAt(1));

            // This value was set in destination. The header in source was ignored.
            Assert.Equal(1, destination.GetValues("custom2").Count());
            Assert.Equal("destination1", destination.GetValues("custom2").First());

            // This value was set in destination. The header in source was ignored.
            Assert.Equal(1, destination.GetValues("known1").Count());
            Assert.Equal(parsedPrefix + "9", destination.GetValues("known1").First());

            // The header in source gets first parsed and then copied to destination. Note that here we have one
            // invalid value.
            Assert.Equal(3, destination.GetValues("known2").Count());
            Assert.Equal(parsedPrefix + "5", destination.GetValues("known2").ElementAt(0));
            Assert.Equal(parsedPrefix + "7", destination.GetValues("known2").ElementAt(1));
            Assert.Equal(invalidHeaderValue, destination.GetValues("known2").ElementAt(2));

            // Header 'known3' should not be copied, since it doesn't contain any values.
            Assert.False(destination.Contains("known3"), "'known3' header value count.");

            Assert.Equal(2, destination.GetValues("known4").Count());
            Assert.Equal(known4Value1.ToString(), destination.GetValues("known4").ElementAt(0));
            Assert.Equal(known4Value2.ToString(), destination.GetValues("known4").ElementAt(1));

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
            Dictionary<string, HttpHeaderParser> parserStore = new Dictionary<string, HttpHeaderParser>();
            parserStore.Add("known1", new MockHeaderParser());

            MockHeaders source = new MockHeaders(parserStore);

            MockHeaders destination = new MockHeaders(parserStore);
            destination.Add("known1", rawPrefix);

            destination.AddHeaders(source);

            Assert.Equal(1, destination.Count());
        }

        [Fact]
        public void AddHeaders_DestinationHasEmptyHeaderStore_DestinationHeaderStoreGetsCreatedAndValuesAdded()
        {
            Dictionary<string, HttpHeaderParser> parserStore = new Dictionary<string, HttpHeaderParser>();
            parserStore.Add("known1", new MockHeaderParser());

            MockHeaders source = new MockHeaders(parserStore);
            source.Add("known1", rawPrefix);

            MockHeaders destination = new MockHeaders(parserStore);

            destination.AddHeaders(source);

            Assert.Equal(1, destination.Count());
        }

        [Fact]
        public void AddHeaders_SourceHasInvalidHeaderValues_InvalidHeadersRemovedFromSourceAndNotCopiedToDestination()
        {
            Dictionary<string, HttpHeaderParser> parserStore = new Dictionary<string, HttpHeaderParser>();
            parserStore.Add("known", new MockHeaderParser());

            MockHeaders source = new MockHeaders(parserStore);
            source.TryAddWithoutValidation("known", invalidHeaderValue + "\r\ninvalid");
            source.TryAddWithoutValidation("custom", "invalid\r\nvalue");

            MockHeaders destination = new MockHeaders(parserStore);
            destination.AddHeaders(source);

            Assert.Equal(0, source.Count());
            Assert.False(source.Contains("known"), "source contains 'known' header.");
            Assert.False(source.Contains("custom"), "source contains 'custom' header.");
            Assert.Equal(0, destination.Count());
            Assert.False(destination.Contains("known"), "destination contains 'known' header.");
            Assert.False(destination.Contains("custom"), "destination contains 'custom' header.");
        }

        #region Helper methods

        private class MockHeaders : HttpHeaders
        {
            private MockHeaderParser parser;

            public MockHeaderParser Parser
            {
                get { return parser; }
            }

            public MockHeaders(MockHeaderParser parser)
                : base()
            {
                Dictionary<string, HttpHeaderParser> parserStore = new Dictionary<string, HttpHeaderParser>();
                this.parser = parser;
                parserStore.Add(knownHeader, this.parser);
                SetConfiguration(parserStore, new HashSet<string>());
            }

            public MockHeaders()
                : this(new MockHeaderParser())
            {
            }

            public MockHeaders(Dictionary<string, HttpHeaderParser> parserStore)
            {
                SetConfiguration(parserStore, new HashSet<string>());
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
                }
                else if (tempValue.StartsWith(invalidHeaderValue, StringComparison.Ordinal))
                {
                    return false;
                }
                else
                {
                    Assert.True(false, string.Format("Unknown mock value: {0}", tempValue));
                }

                return true;
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

            public CustomTypeHeaders(string headerName, HttpHeaderParser parser)
            {
                Dictionary<string, HttpHeaderParser> parserStore = new Dictionary<string, HttpHeaderParser>();
                parserStore.Add(headerName, parser);
                SetConfiguration(parserStore, new HashSet<string>());
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
