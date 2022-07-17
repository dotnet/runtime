// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailAddressParserTest
    {
        private const string WhitespaceOnly = "   \t\t \r\n  ";
        private const string NoWhitespace = "asbddfhil";
        private const string WhitespaceInMiddle = "asd  \tsdf\r\nasdf d";
        private const string WhitespaceAtBeginning = "    asdf";
        private const string NoComments = "asdke4fioj  sdfk";
        private const string OneComment = "(comments with stuff)";
        private const string NestedComments = "(a(b) cc (dd))";
        private const string OneCommentWithAdditionalCharsBefore = "asdf(comment)";
        private const string SingleSpace = " ";
        private const string InvalidDomainLiteral = "[something invalid \r\n ] characters ]";
        private const string ValidDomainLiteral = "[ test]";
        private const string ValidDomainLiteralEscapedChars = "[something \\] that has \\\\ escaped chars]";
        private const string ValidDomainLiteralEscapedCharsResult = "[something ] that has \\ escaped chars]";
        private const string ValidDomainBackslashList = "[ab\\\\\\da]";
        private const string ValidDomainBackslashListResult = "[ab\\da]";
        private const string ValidQuotedString = "\"I am a quoted string\"";
        private const string ValidMultipleStrings = "A Q S, I am a string";
        private const string ValidMultipleQuotedStrings = "\"A Q S\", \"I am a string\"";
        private const string ValidQuotedStringWithEscapedChars = "\"quoted \\\\ \\\" string\"";
        private const string ValidQuotedStringWithEscapedCharsResult = "\"quoted \\ \" string\"";
        private const string InvalidQuotedString = "I am not valid\"";
        private const string UnicodeQuotedString = "I have \u3069 unicode";
        private const string ValidDotAtom = " a.something#-text";
        private const string ValidDotAtomResult = "a.something#-text";
        private const string ValidDotAtomDoubleDots = " a.d....d";
        private const string ValidDotAtomDoubleDotsResult = "a.d....d";
        private const string ValidDotAtomEndsInDot = "a.something.";
        private const string InvalidDotAtom = "a.something\"test";
        private const string InvalidDotAtomStartsWithDot = ".test";
        private const string ValidEmailAddressWithDisplayName = "\"jeff\" <jetucker@microsoft.com>";
        private const string ValidEmailAddressWithNoDisplayName = "<jetucker@microsoft.com>";
        private const string ValidEmailAddressWithNoAngleBrackets = "jetucker@microsoft.com";
        private const string ValidEmailAddressWithDomainLiteral = "\"jeff\" <jetucker@[example]>";
        private const string ValidEmailAddressQuotedLocal = "\"jeff\"@example.com";

        [Fact]
        public void TryReadFWS_WithOnlyWhiteSpace_ShouldReadAll()
        {
            int index = WhitespaceOnly.Length - 1;
            Assert.True(WhitespaceReader.TryReadFwsReverse(WhitespaceOnly, index, out index, throwExceptionIfFail: true));

            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadFWS_WithNoWhiteSpace_ShouldDoNothing()
        {
            int index = NoWhitespace.Length - 1;

            Assert.True(WhitespaceReader.TryReadFwsReverse(NoWhitespace, index, out index, throwExceptionIfFail: true));
            Assert.Equal(NoWhitespace.Length - 1, index);

            index = WhitespaceInMiddle.Length - 1;

            Assert.True(WhitespaceReader.TryReadFwsReverse(WhitespaceInMiddle, index, out index, throwExceptionIfFail: true));
            Assert.Equal(WhitespaceInMiddle.Length - 1, index);
        }

        [Fact]
        public void TryReadFWS_AtBeginningOfString_ShouldReadFWS()
        {
            int index = 3;

            Assert.True(WhitespaceReader.TryReadFwsReverse(WhitespaceAtBeginning, index, out index, throwExceptionIfFail: true));
            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadCFWS_WithSingleSpace_ShouldReadTheSpace()
        {
            int index = SingleSpace.Length - 1;

            Assert.True(WhitespaceReader.TryReadCfwsReverse(SingleSpace, index, out index, throwExceptionIfFail: true));
            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadCFWS_WithNoComment_ShouldDoNothing()
        {
            int index = NoComments.Length - 1;

            Assert.True(WhitespaceReader.TryReadCfwsReverse(NoComments, index, out index, throwExceptionIfFail: true));
            Assert.Equal(index, NoComments.Length - 1);
        }

        [Fact]
        public void TryReadCFWS_WithOnlyComment_ShouldReadAll()
        {
            int index = OneComment.Length - 1;

            Assert.True(WhitespaceReader.TryReadCfwsReverse(OneComment, index, out index, throwExceptionIfFail: true));
            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadCFWS_WithNestedComments_ShouldWorkCorrectly()
        {
            int index = NestedComments.Length - 1;

            Assert.True(WhitespaceReader.TryReadCfwsReverse(NestedComments, index, out index, throwExceptionIfFail: true));
            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadCFWS_WithCharsBeforeComment_ShouldWorkCorrectly()
        {
            int index = OneCommentWithAdditionalCharsBefore.Length - 1;

            Assert.True(WhitespaceReader.TryReadCfwsReverse(OneCommentWithAdditionalCharsBefore, index, out index, throwExceptionIfFail: true));
            Assert.Equal(3, index);
        }

        [Fact]
        public void TryReadQuotedPair_WithValidCharacters_ShouldReadCorrectly()
        {
            // a\\\\b, even quotes, b is unqouted
            string backslashes = "a\\\\\\\\b";
            int index = backslashes.Length - 1;
            Assert.True(QuotedPairReader.TryCountQuotedChars(backslashes, index, false, out int quotedCharCount, throwExceptionIfFail: true));

            Assert.Equal(0, quotedCharCount);
        }

        [Fact]
        public void TryReadQuotedPair_WithValidCharacterAndEscapedCharacter_ShouldReadCorrectly()
        {
            // this is a\\\\\"
            string backslashes = "a\\\\\\\\\\\"";
            int index = backslashes.Length - 1;
            Assert.True(QuotedPairReader.TryCountQuotedChars(backslashes, index, false, out int quotedCharCount, throwExceptionIfFail: true));

            Assert.Equal(6, quotedCharCount);
        }

        [Fact]
        public void TryReadQuotedPair_WithValidCharacterAndEscapedCharacterAndBackslashIsStartOfString_ShouldReadCorrectly()
        {
            // this is \\\\\"
            string backslashes = "\\\\\\\\\\\"";
            int index = backslashes.Length - 1;
            Assert.True(QuotedPairReader.TryCountQuotedChars(backslashes, index, false, out int quotedCharCount, throwExceptionIfFail: true));

            Assert.Equal(6, quotedCharCount);
        }

        [Fact]
        public void TryReadQuotedPair_WithInvalidNonescapedCharacter_ShouldThrow()
        {
            // this is a\\\\"
            string backslashes = "a\\\\\\\\\"";
            int index = backslashes.Length - 1;
            Assert.True(QuotedPairReader.TryCountQuotedChars(backslashes, index, false, out int quotedCharCount, throwExceptionIfFail: true));

            Assert.Equal(0, quotedCharCount);
        }

        [Fact]
        public void TryReadDomainLiteral_WithValidDomainLiteral_ShouldReadCorrectly()
        {
            int index = ValidDomainLiteral.Length - 1;
            Assert.True(DomainLiteralReader.TryReadReverse(ValidDomainLiteral, index, out index, throwExceptionIfFail: true));

            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadDomainLiteral_WithLongListOfBackslashes_ShouldReadCorrectly()
        {
            int index = ValidDomainBackslashList.Length - 1;
            Assert.True(DomainLiteralReader.TryReadReverse(ValidDomainBackslashList, index, out index, throwExceptionIfFail: true));

            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadDomainLiteral_WithValidDomainLiteralAndEscapedCharacters_ShouldReadCorrectly()
        {
            int index = ValidDomainLiteralEscapedChars.Length - 1;
            Assert.True(DomainLiteralReader.TryReadReverse(ValidDomainLiteralEscapedChars, index, out index, throwExceptionIfFail: true));

            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadDomainLiteral_WithInvalidCharacter_ShouldThrow()
        {
            int index = InvalidDomainLiteral.Length - 1;
            Assert.Throws<FormatException>(() => { DomainLiteralReader.TryReadReverse(InvalidDomainLiteral, index, out int _, throwExceptionIfFail: true); });
        }

        [Fact]
        public void TryReadQuotedString_WithUnicodeAndUnicodeIsInvalid_ShouldThrow()
        {
            int index = UnicodeQuotedString.Length - 1;
            Assert.Throws<FormatException>(() => { QuotedStringFormatReader.TryReadReverseUnQuoted(UnicodeQuotedString, index, false, false, out int _, throwExceptionIfFail: true); });
        }

        [Fact]
        public void TryReadQuotedString_WithValidUnicodeQuotedString_ShouldReadCorrectly()
        {
            int index = UnicodeQuotedString.Length - 1;
            Assert.True(QuotedStringFormatReader.TryReadReverseUnQuoted(UnicodeQuotedString, index, true, false, out index, throwExceptionIfFail: true));

            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadQuotedString_WithValidQuotedString_ShouldReadCorrectly()
        {
            int index = ValidQuotedString.Length - 1;
            Assert.True(QuotedStringFormatReader.TryReadReverseQuoted(ValidQuotedString, index, false, out index, throwExceptionIfFail: true));

            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadQuotedString_WithEscapedCharacters_ShouldReadCorrectly()
        {
            int index = ValidQuotedStringWithEscapedChars.Length - 1;
            Assert.True(QuotedStringFormatReader.TryReadReverseQuoted(ValidQuotedStringWithEscapedChars, index, false, out index, throwExceptionIfFail: true));

            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadQuotedString_WithInvalidCharacters_ShouldThrow()
        {
            int index = InvalidQuotedString.Length - 1;
            Assert.Throws<FormatException>(() => { QuotedStringFormatReader.TryReadReverseQuoted(InvalidQuotedString, index, false, out int _, throwExceptionIfFail: true); });
        }

        [Fact]
        public void TryReadQuotedString_WithValidMultipleStrings_ReturnTheDelimterIndex()
        {
            int index = ValidMultipleStrings.Length - 1;
            Assert.True(QuotedStringFormatReader.TryReadReverseUnQuoted(ValidMultipleStrings, index, false, true, out index, throwExceptionIfFail: true));

            Assert.Equal(5, index);
        }

        [Fact]
        public void TryReadQuotedString_WithValidMultipleQuotedStrings_ReturnTheIndexPastTheQuote()
        {
            int index = ValidMultipleQuotedStrings.Length - 1;
            Assert.True(QuotedStringFormatReader.TryReadReverseQuoted(ValidMultipleQuotedStrings, index, false, out index, throwExceptionIfFail: true));

            Assert.Equal(8, index);
        }

        [Fact]
        public void TryReadDotAtom_WithValidDotAtom_ShouldReadCorrectly()
        {
            int index = ValidDotAtom.Length - 1;
            Assert.True(DotAtomReader.TryReadReverse(ValidDotAtom, index, out index, throwExceptionIfFail: true));

            Assert.Equal(0, index);
        }

        [Fact]
        public void TryReadDotAtom_WithValidDotAtomAndDoubleDots_ShouldReadCorrectly()
        {
            int index = ValidDotAtomDoubleDots.Length - 1;
            Assert.True(DotAtomReader.TryReadReverse(ValidDotAtomDoubleDots, index, out index, throwExceptionIfFail: true));

            Assert.Equal(0, index);
        }

        [Fact]
        public void TryReadDotAtom_EndsInDot_ShouldReadCorrectly()
        {
            int index = ValidDotAtomEndsInDot.Length - 1;
            Assert.True(DotAtomReader.TryReadReverse(ValidDotAtomEndsInDot, index, out index, throwExceptionIfFail: true));

            Assert.Equal(-1, index);
        }

        [Fact]
        public void TryReadDotAtom_WithDotAtBeginning_ShouldThrow()
        {
            int index = InvalidDotAtomStartsWithDot.Length - 1;
            Assert.Throws<FormatException>(() => { DotAtomReader.TryReadReverse(InvalidDotAtomStartsWithDot, index, out int _, throwExceptionIfFail: true); });
        }

        [Fact]
        public void TryParseAddress_WithNoQuotes_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress("test(comment) test@example.com", out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal("test", result.User);
            Assert.Equal("example.com", result.Host);
            Assert.Equal("test", result.DisplayName);
        }

        [Fact]
        public void TryParseAddress_WithDisplayNameAndAddress_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress(ValidEmailAddressWithDisplayName, out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal("jeff", result.DisplayName);
            Assert.Equal("jetucker", result.User);
            Assert.Equal("microsoft.com", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithNoDisplayNameAndAngleAddress_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress(ValidEmailAddressWithNoDisplayName, out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("jetucker", result.User);
            Assert.Equal("microsoft.com", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithNoDisplayNameAndNoAngleBrackets_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress(ValidEmailAddressWithNoAngleBrackets, out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("jetucker", result.User);
            Assert.Equal("microsoft.com", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithDomainLiteral_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress(ValidEmailAddressWithDomainLiteral, out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal("jeff", result.DisplayName);
            Assert.Equal("jetucker", result.User);
            Assert.Equal("[example]", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithQuotedLocalPartAndNoDisplayName_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress(ValidEmailAddressQuotedLocal, out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("\"jeff\"", result.User);
            Assert.Equal("example.com", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithCommentsAndQuotedLocalAndNoDisplayName_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress("(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)this.test.this(comment)", out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("\" asciin;,oqu o.tesws \"", result.User);
            Assert.Equal("this.test.this", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithCommentsAndUnquotedLocalAndUnquotedDisplayName_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress("(comment)this.test.this(comment)<(comment)this.test.this(comment)@(comment)[  test this ](comment)>", out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal("(comment)this.test.this(comment)", result.DisplayName);
            Assert.Equal("this.test.this", result.User);
            Assert.Equal("[  test this ]", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithEscapedCharacters_AndQuotedLocalPart_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress("\"jeff\\\\@\" <(jeff\\@s email)\"jeff\\\"\"@(comment)[  ncl\\@bld-001 \t  ](comment)>", out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal("jeff\\\\@", result.DisplayName);
            Assert.Equal("\"jeff\\\"\"", result.User);
            Assert.Equal("[  ncl\\@bld-001 \t  ]", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithNoDisplayNameAndDotAtom_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress("a..b_b@example.com", out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("a..b_b", result.User);
            Assert.Equal("example.com", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithQuotedDisplayNameandNoAngleAddress_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress("\"Test user\" testuser@nclmailtest.com", out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal("Test user", result.DisplayName);
            Assert.Equal("testuser", result.User);
            Assert.Equal("nclmailtest.com", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithInvalidLocalPart_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => { MailAddressParser.TryParseAddress("test[test]@test.com", out ParseAddressInfo _, true); });
        }

        [Fact]
        public void TryParseAddress_WithHangingAngleBracket_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => { MailAddressParser.TryParseAddress("<test@test.com", out ParseAddressInfo _, true); });
        }

        [Fact]
        public void TryParseAddress_WithQuotedDisplayNameAndQuotedLocalAndAngleBrackets_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress("(comment)\" asciin;,oqu o.tesws \"(comment)<(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)[  test this ](comment)>", out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal(" asciin;,oqu o.tesws ", result.DisplayName);
            Assert.Equal("\" asciin;,oqu o.tesws \"", result.User);
            Assert.Equal("[  test this ]", result.Host);
        }

        [Fact]
        public void TryParseAddress_WithInvalidLocalPartAtEnd_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => { MailAddressParser.TryParseAddress("[test]test@test.com", out ParseAddressInfo _, true); });
        }

        [Fact]
        public void TryParseAddress_WithCommaButNoQuotes_ShouldReadCorrectly()
        {
            Assert.True(MailAddressParser.TryParseAddress("unqouted, comma display username@domain", out ParseAddressInfo result, throwExceptionIfFail: true));

            Assert.Equal("username", result.User);
            Assert.Equal("domain", result.Host);
            Assert.Equal("unqouted, comma display", result.DisplayName);
        }

        [Fact]
        public void MailAddress_WithDisplayNameParameterQuotes_ShouldReadCorrectly()
        {
            MailAddress result = new MailAddress("display username@domain", "\"quoted display\"");

            Assert.Equal("username", result.User);
            Assert.Equal("domain", result.Host);
            Assert.Equal("quoted display", result.DisplayName);
        }

        [Fact]
        public void MailAddress_WithDisplayNameParameterNoQuotes_ShouldReadCorrectly()
        {
            MailAddress result = new MailAddress("display username@domain", "quoted display");

            Assert.Equal("username", result.User);
            Assert.Equal("domain", result.Host);
            Assert.Equal("quoted display", result.DisplayName);
        }

        [Fact]
        public void MailAddress_NeedsUnicodeNormalization_ShouldParseAndNormalize()
        {
            string needsNormalization = "\u0063\u0301\u0327\u00BE";
            string normalized = "\u1E09\u00BE";
            MailAddress result = new MailAddress(
                string.Format("display{0}name user{0}name@domain{0}name", needsNormalization));

            Assert.Equal("user" + normalized + "name", result.User);
            Assert.Equal("domain" + normalized + "name", result.Host);
            Assert.Equal("display" + normalized + "name", result.DisplayName);
        }

        [Fact]
        public void MailAddress_NeedsUnicodeNormalizationWithDisplayName_ShouldParseAndNormalize()
        {
            string needsNormalization = "\u0063\u0301\u0327\u00BE";
            string normalized = "\u1E09\u00BE";
            MailAddress result = new MailAddress(
                string.Format("display{0}name user{0}name@domain{0}name", needsNormalization),
                "second display" + needsNormalization + "name");

            Assert.Equal("user" + normalized + "name", result.User);
            Assert.Equal("domain" + normalized + "name", result.Host);
            Assert.Equal("second display" + normalized + "name", result.DisplayName);
        }

        [Fact]
        public void ParseAddress_WithMultipleSimpleAddresses_ShouldReadCorrectly()
        {
            IList<MailAddress> result;
            string addresses = string.Format("{0},{1}", "jeff@example.com", "jeff2@example.org");
            result = MailAddressParser.ParseMultipleAddresses(addresses);

            Assert.Equal(2, result.Count);

            Assert.Equal(string.Empty, result[0].DisplayName);
            Assert.Equal("jeff", result[0].User);
            Assert.Equal("example.com", result[0].Host);

            Assert.Equal(string.Empty, result[1].DisplayName);
            Assert.Equal("jeff2", result[1].User);
            Assert.Equal("example.org", result[1].Host);
        }

        [Fact]
        public void ParseAddresses_WithOnlyOneAddress_ShouldReadCorrectly()
        {
            IList<MailAddress> result = MailAddressParser.ParseMultipleAddresses("Dr M\u00FCller <test@mail.com>");

            Assert.Equal(1, result.Count);
            Assert.Equal("Dr M\u00FCller", result[0].DisplayName);
            Assert.Equal("test", result[0].User);
            Assert.Equal("mail.com", result[0].Host);
        }

        [Fact]
        public void ParseAddresses_WithManyComplexAddresses_ShouldReadCorrectly()
        {
            string addresses = string.Format("{0},{1},{2},{3},{4},{5},{6}",
                "\"Dr M\u00FCller\" test@mail.com",
                "(comment)this.test.this(comment)@(comment)this.test.this(comment)",
                "jeff@example.com",
                "jeff2@example.org",
                "(comment)this.test.this(comment)<(comment)this.test.this(comment)@(comment)[  test this ](comment)>",
                "\"test\" <a..b_b@example.com>",
                "(comment)\" asciin;,oqu o.tesws \"(comment)<(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)this.test.this(comment)>");

            IList<MailAddress> result = MailAddressParser.ParseMultipleAddresses(addresses);

            Assert.Equal(7, result.Count);

            Assert.Equal("Dr M\u00FCller", result[0].DisplayName);
            Assert.Equal("test", result[0].User);
            Assert.Equal("mail.com", result[0].Host);

            Assert.Equal(string.Empty, result[1].DisplayName);
            Assert.Equal("this.test.this", result[1].User);
            Assert.Equal("this.test.this", result[1].Host);

            Assert.Equal(string.Empty, result[2].DisplayName);
            Assert.Equal("jeff", result[2].User);
            Assert.Equal("example.com", result[2].Host);

            Assert.Equal(string.Empty, result[3].DisplayName);
            Assert.Equal("jeff2", result[3].User);
            Assert.Equal("example.org", result[3].Host);

            Assert.Equal("(comment)this.test.this(comment)", result[4].DisplayName);
            Assert.Equal("this.test.this", result[4].User);
            Assert.Equal("[  test this ]", result[4].Host);

            Assert.Equal("test", result[5].DisplayName);
            Assert.Equal("a..b_b", result[5].User);
            Assert.Equal("example.com", result[5].Host);

            Assert.Equal(" asciin;,oqu o.tesws ", result[6].DisplayName);
            Assert.Equal("\" asciin;,oqu o.tesws \"", result[6].User);
            Assert.Equal("this.test.this", result[6].Host);
        }
    }
}
