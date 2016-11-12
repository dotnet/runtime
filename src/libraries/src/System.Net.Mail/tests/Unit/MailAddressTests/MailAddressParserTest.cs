// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public void ReadFWS_WithOnlyWhiteSpace_ShouldReadAll()
        {
            int index = WhitespaceOnly.Length - 1;
            index = WhitespaceReader.ReadFwsReverse(WhitespaceOnly, index);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadFWS_WithNoWhiteSpace_ShouldDoNothing()
        {
            int index = NoWhitespace.Length - 1;

            index = WhitespaceReader.ReadFwsReverse(NoWhitespace, index);
            Assert.Equal(NoWhitespace.Length - 1, index);

            index = WhitespaceInMiddle.Length - 1;

            index = WhitespaceReader.ReadFwsReverse(WhitespaceInMiddle, index);
            Assert.Equal(WhitespaceInMiddle.Length - 1, index);
        }

        [Fact]
        public void ReadFWS_AtBeginningOfString_ShouldReadFWS()
        {
            int index = 3;

            index = WhitespaceReader.ReadFwsReverse(WhitespaceAtBeginning, index);
            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadCFWS_WithSingleSpace_ShouldReadTheSpace()
        {
            int index = SingleSpace.Length - 1;

            index = WhitespaceReader.ReadCfwsReverse(SingleSpace, index);
            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadCFWS_WithNoComment_ShouldDoNothing()
        {
            int index = NoComments.Length - 1;

            index = WhitespaceReader.ReadCfwsReverse(NoComments, index);
            Assert.Equal(index, NoComments.Length - 1);
        }

        [Fact]
        public void ReadCFWS_WithOnlyComment_ShouldReadAll()
        {
            int index = OneComment.Length - 1;

            index = WhitespaceReader.ReadCfwsReverse(OneComment, index);
            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadCFWS_WithNestedComments_ShouldWorkCorrectly()
        {
            int index = NestedComments.Length - 1;

            index = WhitespaceReader.ReadCfwsReverse(NestedComments, index);
            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadCFWS_WithCharsBeforeComment_ShouldWorkCorrectly()
        {
            int index = OneCommentWithAdditionalCharsBefore.Length - 1;

            index = WhitespaceReader.ReadCfwsReverse(OneCommentWithAdditionalCharsBefore, index);
            Assert.Equal(3, index);
        }

        [Fact]
        public void ReadQuotedPair_WithValidCharacters_ShouldReadCorrectly()
        {
            // a\\\\b, even quotes, b is unqouted
            string backslashes = "a\\\\\\\\b";
            int index = backslashes.Length - 1;
            int quotedCharCount = QuotedPairReader.CountQuotedChars(backslashes, index, false);

            Assert.Equal(0, quotedCharCount);
        }

        [Fact]
        public void ReadQuotedPair_WithValidCharacterAndEscapedCharacter_ShouldReadCorrectly()
        {
            // this is a\\\\\"
            string backslashes = "a\\\\\\\\\\\"";
            int index = backslashes.Length - 1;
            int quotedCharCount = QuotedPairReader.CountQuotedChars(backslashes, index, false);

            Assert.Equal(6, quotedCharCount);
        }

        [Fact]
        public void ReadQuotedPair_WithValidCharacterAndEscapedCharacterAndBackslashIsStartOfString_ShouldReadCorrectly()
        {
            // this is \\\\\"
            string backslashes = "\\\\\\\\\\\"";
            int index = backslashes.Length - 1;
            int quotedCharCount = QuotedPairReader.CountQuotedChars(backslashes, index, false);

            Assert.Equal(6, quotedCharCount);
        }

        [Fact]
        public void ReadQuotedPair_WithInvalidNonescapedCharacter_ShouldThrow()
        {
            // this is a\\\\"
            string backslashes = "a\\\\\\\\\"";
            int index = backslashes.Length - 1;
            int quotedCharCount = QuotedPairReader.CountQuotedChars(backslashes, index, false);

            Assert.Equal(0, quotedCharCount);
        }

        [Fact]
        public void ReadDomainLiteral_WithValidDomainLiteral_ShouldReadCorrectly()
        {
            int index = ValidDomainLiteral.Length - 1;
            index = DomainLiteralReader.ReadReverse(ValidDomainLiteral, index);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadDomainLiteral_WithLongListOfBackslashes_ShouldReadCorrectly()
        {
            int index = ValidDomainBackslashList.Length - 1;
            index = DomainLiteralReader.ReadReverse(ValidDomainBackslashList, index);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadDomainLiteral_WithValidDomainLiteralAndEscapedCharacters_ShouldReadCorrectly()
        {
            int index = ValidDomainLiteralEscapedChars.Length - 1;
            index = DomainLiteralReader.ReadReverse(ValidDomainLiteralEscapedChars, index);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadDomainLiteral_WithInvalidCharacter_ShouldThrow()
        {
            int index = InvalidDomainLiteral.Length - 1;
            Assert.Throws<FormatException>(() => { DomainLiteralReader.ReadReverse(InvalidDomainLiteral, index); });
        }

        [Fact]
        public void ReadQuotedString_WithUnicodeAndUnicodeIsInvalid_ShouldThrow()
        {
            int index = UnicodeQuotedString.Length - 1;
            Assert.Throws<FormatException>(() => { QuotedStringFormatReader.ReadReverseUnQuoted(UnicodeQuotedString, index, false, false); });
        }

        [Fact]
        public void ReadQuotedString_WithValidUnicodeQuotedString_ShouldReadCorrectly()
        {
            int index = UnicodeQuotedString.Length - 1;
            index = QuotedStringFormatReader.ReadReverseUnQuoted(UnicodeQuotedString, index, true, false);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadQuotedString_WithValidQuotedString_ShouldReadCorrectly()
        {
            int index = ValidQuotedString.Length - 1;
            index = QuotedStringFormatReader.ReadReverseQuoted(ValidQuotedString, index, false);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadQuotedString_WithEscapedCharacters_ShouldReadCorrectly()
        {
            int index = ValidQuotedStringWithEscapedChars.Length - 1;
            index = QuotedStringFormatReader.ReadReverseQuoted(ValidQuotedStringWithEscapedChars, index, false);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadQuotedString_WithInvalidCharacters_ShouldThrow()
        {
            int index = InvalidQuotedString.Length - 1;
            Assert.Throws<FormatException>(() => { QuotedStringFormatReader.ReadReverseQuoted(InvalidQuotedString, index, false); });
        }

        [Fact]
        public void ReadQuotedString_WithValidMultipleStrings_ReturnTheDelimterIndex()
        {
            int index = ValidMultipleStrings.Length - 1;
            index = QuotedStringFormatReader.ReadReverseUnQuoted(ValidMultipleStrings, index, false, true);

            Assert.Equal(5, index);
        }

        [Fact]
        public void ReadQuotedString_WithValidMultipleQuotedStrings_ReturnTheIndexPastTheQuote()
        {
            int index = ValidMultipleQuotedStrings.Length - 1;
            index = QuotedStringFormatReader.ReadReverseQuoted(ValidMultipleQuotedStrings, index, false);

            Assert.Equal(8, index);
        }

        [Fact]
        public void ReadDotAtom_WithValidDotAtom_ShouldReadCorrectly()
        {
            int index = ValidDotAtom.Length - 1;
            index = DotAtomReader.ReadReverse(ValidDotAtom, index);

            Assert.Equal(0, index);
        }

        [Fact]
        public void ReadDotAtom_WithValidDotAtomAndDoubleDots_ShouldReadCorrectly()
        {
            int index = ValidDotAtomDoubleDots.Length - 1;
            index = DotAtomReader.ReadReverse(ValidDotAtomDoubleDots, index);

            Assert.Equal(0, index);
        }

        [Fact]
        public void ReadDotAtom_EndsInDot_ShouldReadCorrectly()
        {
            int index = ValidDotAtomEndsInDot.Length - 1;
            index = DotAtomReader.ReadReverse(ValidDotAtomEndsInDot, index);

            Assert.Equal(-1, index);
        }

        [Fact]
        public void ReadDotAtom_WithDotAtBeginning_ShouldThrow()
        {
            int index = InvalidDotAtomStartsWithDot.Length - 1;
            Assert.Throws<FormatException>(() => { DotAtomReader.ReadReverse(InvalidDotAtomStartsWithDot, index); });
        }

        [Fact]
        public void ParseAddress_WithNoQuotes_ShouldReadCorrectly()
        {
            MailAddress result = MailAddressParser.ParseAddress("test(comment) test@example.com");

            Assert.Equal("test", result.User);
            Assert.Equal("example.com", result.Host);
            Assert.Equal("test", result.DisplayName);
        }

        [Fact]
        public void ParseAddress_WithDisplayNameAndAddress_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress(ValidEmailAddressWithDisplayName);

            Assert.Equal("jeff", result.DisplayName);
            Assert.Equal("jetucker", result.User);
            Assert.Equal("microsoft.com", result.Host);
        }

        [Fact]
        public void ParseAddress_WithNoDisplayNameAndAngleAddress_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress(ValidEmailAddressWithNoDisplayName);

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("jetucker", result.User);
            Assert.Equal("microsoft.com", result.Host);
        }

        [Fact]
        public void ParseAddress_WithNoDisplayNameAndNoAngleBrackets_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress(ValidEmailAddressWithNoAngleBrackets);

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("jetucker", result.User);
            Assert.Equal("microsoft.com", result.Host);
        }

        [Fact]
        public void ParseAddress_WithDomainLiteral_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress(ValidEmailAddressWithDomainLiteral);

            Assert.Equal("jeff", result.DisplayName);
            Assert.Equal("jetucker", result.User);
            Assert.Equal("[example]", result.Host);
        }

        [Fact]
        public void ParseAddress_WithQuotedLocalPartAndNoDisplayName_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress(ValidEmailAddressQuotedLocal);

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("\"jeff\"", result.User);
            Assert.Equal("example.com", result.Host);
        }

        [Fact]
        public void ParseAddress_WithCommentsAndQuotedLocalAndNoDisplayName_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress("(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)this.test.this(comment)");

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("\" asciin;,oqu o.tesws \"", result.User);
            Assert.Equal("this.test.this", result.Host);
        }

        [Fact]
        public void ParseAddress_WithCommentsAndUnquotedLocalAndUnquotedDisplayName_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress("(comment)this.test.this(comment)<(comment)this.test.this(comment)@(comment)[  test this ](comment)>");

            Assert.Equal("(comment)this.test.this(comment)", result.DisplayName);
            Assert.Equal("this.test.this", result.User);
            Assert.Equal("[  test this ]", result.Host);
        }

        [Fact]
        public void ParseAddress_WithEscapedCharacters_AndQuotedLocalPart_ShouldReadCorrectly()
        {
            MailAddress result = MailAddressParser.ParseAddress("\"jeff\\\\@\" <(jeff\\@s email)\"jeff\\\"\"@(comment)[  ncl\\@bld-001 \t  ](comment)>");

            Assert.Equal("jeff\\\\@", result.DisplayName);
            Assert.Equal("\"jeff\\\"\"", result.User);
            Assert.Equal("[  ncl\\@bld-001 \t  ]", result.Host);
        }

        [Fact]
        public void ParseAddress_WithNoDisplayNameAndDotAtom_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress("a..b_b@example.com");

            Assert.Equal(string.Empty, result.DisplayName);
            Assert.Equal("a..b_b", result.User);
            Assert.Equal("example.com", result.Host);
        }

        [Fact]
        public void ParseAddress_WithQuotedDisplayNameandNoAngleAddress_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress("\"Test user\" testuser@nclmailtest.com");

            Assert.Equal("Test user", result.DisplayName);
            Assert.Equal("testuser", result.User);
            Assert.Equal("nclmailtest.com", result.Host);
        }

        [Fact]
        public void ParseAddress_WithInvalidLocalPart_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => { MailAddressParser.ParseAddress("test[test]@test.com"); });
        }

        [Fact]
        public void ParseAddress_WithHangingAngleBracket_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => { MailAddressParser.ParseAddress("<test@test.com"); });
        }

        [Fact]
        public void ParseAddress_WithQuotedDisplayNameAndQuotedLocalAndAngleBrackets_ShouldReadCorrectly()
        {
            MailAddress result;
            result = MailAddressParser.ParseAddress("(comment)\" asciin;,oqu o.tesws \"(comment)<(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)[  test this ](comment)>");

            Assert.Equal(" asciin;,oqu o.tesws ", result.DisplayName);
            Assert.Equal("\" asciin;,oqu o.tesws \"", result.User);
            Assert.Equal("[  test this ]", result.Host);
        }

        [Fact]
        public void ParseAddress_WithInvalidLocalPartAtEnd_ShouldThrow()
        {
            Assert.Throws<FormatException>(() => { MailAddressParser.ParseAddress("[test]test@test.com"); });
        }

        [Fact]
        public void ParseAddress_WithCommaButNoQuotes_ShouldReadCorrectly()
        {
            MailAddress result = MailAddressParser.ParseAddress("unqouted, comma display username@domain");

            Assert.Equal("username", result.User);
            Assert.Equal("domain", result.Host);
            Assert.Equal("unqouted, comma display", result.DisplayName);
        }

        [Fact]
        public void MailAddress_WithDisplayNameParamiterQuotes_ShouldReadCorrectly()
        {
            MailAddress result = new MailAddress("display username@domain", "\"quoted display\"");

            Assert.Equal("username", result.User);
            Assert.Equal("domain", result.Host);
            Assert.Equal("quoted display", result.DisplayName);
        }

        [Fact]
        public void MailAddress_WithDisplayNameParamiterNoQuotes_ShouldReadCorrectly()
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
        public void ParseAdresses_WithOnlyOneAddress_ShouldReadCorrectly()
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
