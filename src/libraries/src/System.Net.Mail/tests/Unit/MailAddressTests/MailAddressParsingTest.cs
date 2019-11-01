// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailAddressParsingTest
    {
        public static IEnumerable<object[]> GetValidEmailTestData()
        {
            yield return new object[] { "(comment)this.test.this(comment)<(comment)this.test.this(comment)@(comment)this.test.this(comment)>" };
            yield return new object[] { "(comment)\" asciin;,oqu o.tesws \"(comment)<(comment)this.test.this(comment)@(comment)this.test.this(comment)>" };
            yield return new object[] { "(comment)this.test.this(comment)<(comment)this.test.this(comment)@(comment)[  test this ](comment)>" };
            yield return new object[] { "(comment)\" asciin;,oqu o.tesws \"(comment)<(comment)this.test.this(comment)@(comment)[  test this ](comment)>" };
            yield return new object[] { "(comment)this.test.this(comment)<(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)[  test this ](comment)>" };
            yield return new object[] { "(comment)this(comment)<(comment)\" asciin);,oqu o.tesws \"(comment)@(comment)[  test this ](comment)>" };
            yield return new object[] { "(comment)\" asciin;,oqu o.tesws \"(comment)<(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)[  test this ](comment)>" };
            yield return new object[] { "(comment)this.test.this(comment)<(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)this.test.this(comment)>" };
            yield return new object[] { "(comment)this(comment)<(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)this.test.this(comment)>" };
            yield return new object[] { "(comment)\" asciin;,oqu o.tesws \"(comment)<(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)this.test.this(comment)>" };
            yield return new object[] { "(comment)this.test.this(comment)@(comment)this.test.this(comment)" };
            yield return new object[] { "(comment)this.test.this(comment)@(comment)[  test this ](comment)" };
            yield return new object[] { "(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)[  test this ](comment)" };
            yield return new object[] { "(comment)\" asciin;,oqu o.tesws \"(comment)@(comment)this.test.this(comment)" };
            yield return new object[] { "(comment)displayname(comment) <a@b.c>" };
            yield return new object[] { "a.b <c@d.e>" };
            yield return new object[] { "\"Spaced Out\"@NCLMailTest.com" };
            yield return new object[] { "testUser1@NCLMailTest.com" };
            yield return new object[] { "TestUser3@NCLMailTest.com" };
            yield return new object[] { "to_c@ThisIsNeverWhereISenTIt.com" };
            yield return new object[] { "Test User <testUser1@NCLMailTest.com>" };
            yield return new object[] { "\"Test User\" <testUser@nclmailtest.com>" };
            yield return new object[] { "testuser@[mail.com]" };
            yield return new object[] { "testuser@[ mail.com] " };
            yield return new object[] { "testuser@[mail.com ]" };
            yield return new object[] { "testuser@[mail.com \r\n ]" };
            yield return new object[] { "testuser@[ \r\n mail.com]" };
            yield return new object[] { "testuser@[  mail.com]" };
            yield return new object[] { "testuser <testuser@mail.com>" };
            yield return new object[] { "Test\u3044\u3069 User <testUser1@NCLMailTest.com>" };
            yield return new object[] { "\"Test\u3044\u3069 User\" <testUser@nclmailtest.com>" };
            yield return new object[] { "\"Test\u3044\u3069 user\" testuser@nclmailtest.com" };
            yield return new object[] { "testuser@[mail.com]" };
            yield return new object[] { "testuser\u3044\u3069 <testuser@mail.com>" };
            yield return new object[] { "\"Dr. M\u00FCller\" <test@mail.com>" };
            yield return new object[] { "Dr. M\u00FCller <test@mail.com>" };
            yield return new object[] { "Dr M\u00FCller <test@mail.com>" };
            yield return new object[] { "Dr Muller <test@mail.com>" };
            yield return new object[] { "    leading spaces  <test@mail.com>" };
            yield return new object[] { "\"(nocomment)displayname(nocomment)\"(comment) <a@b.c>" };
            yield return new object[] { "\"Test user\" testuser@nclmailtest.com" };
            yield return new object[] { "\"test\"@test.com" };
            yield return new object[] { "customer/department=shipping@example.com" };
            yield return new object[] { "$A12345@example.com" };
            yield return new object[] { "user+mailbox@example.com" };
            yield return new object[] { "!def!xyz%abc@example.com" };
            yield return new object[] { "_somename@example.com" };
            yield return new object[] { "\"te\\@st\"@example.com" };
            yield return new object[] { "a..b_b@example.com" };
            yield return new object[] { "a..b_b...@example.com" };
            yield return new object[] { "\"test display\" test@(comment)[exam\\@ple](comment)" };
            yield return new object[] { "NoSpaceBeforeEmail\"a\"@example.com" };
            yield return new object[] { "NoSpace BeforeEmail\"a\"@example.com" };
            yield return new object[] { "\"display\"nospace@domain" };
            yield return new object[] { "\"display\"\"nospace\"@domain" };
            yield return new object[] { "\"escaped\\\"quote\" \"a\"@example.com" };
            yield return new object[] { "\"escaped\\\"silly\\\"quotes\" \"a\"@example.com" };
            yield return new object[] { "SquareBracketInUserName \"test[some\"@NCLMailTest.com" };
            yield return new object[] { "SquareBracket[InDispaly \"testsome\"@NCLMailTest.com" };
            yield return new object[] { "\"\\\"QuoteEndedDispaly\" testsome@NCLMailTest.com" };
            yield return new object[] { "\"Quote\\\"MidDispaly\" testsome@NCLMailTest.com" };
            yield return new object[] { "Quote\\\"MidDispaly testsome@NCLMailTest.com" };
            yield return new object[] { "<spaceAfter@NCLMailTest.com>  " };
            yield return new object[] { "spaceAfter@NCLMailTest.com  " };
            yield return new object[] { "<spaceAfter@NCLMailTest.com> (comment) " };
            yield return new object[] { "spaceAfter@NCLMailTest.com (comment) " };
            yield return new object[] { "display (ignoredComment) bracketless@NCLMailTest.com" };
            yield return new object[] { "display (displayComment) <bracket@NCLMailTest.com>" };
            yield return new object[] { "\"display\" (ignoredComment) <bracket@NCLMailTest.com>" };
            yield return new object[] { " \", comma in quotes\" username@domain" };
            yield return new object[] { "user.\"name\"@domain" };
            yield return new object[] { "((NestedComment)) <\"testsome\"@NCLMailTest.com>" };
            yield return new object[] { "<testsome@NCLMailTest.com> (double) (comments)" };
            yield return new object[] { "(Escaped Unicode \\\u3044 Comment) <\"testsome\"@NCLMailTest.com>" };
            yield return new object[] { "(com\\)ment) <\"testsome\"@NCLMailTest.com>" };
            yield return new object[] { "(com \f ment) <\"testsome\"@NCLMailTest.com>" };
            yield return new object[] { "\"disp \f lay\" <\"testsome\"@NCLMailTest.com>" };
            yield return new object[] { "\"EscapedUnicode \\\u3044\\\u3069 display\" <testUser1@NCLMailTest.com>" };
            yield return new object[] { "(Unicode \u3044 Comment) <\"testsome\"@NCLMailTest.com>" };
            yield return new object[] { "\"display \r\n name\" <\"folding\"@domain.com>" };
            yield return new object[] { "\"test\r\n test\"@mail.com" };
            // Email Address Internationalization (EAI)
            yield return new object[] { "UnicodeUserName \"Test\u3044\u3069\"@NCLMailTest.com" };
            yield return new object[] { "<\"EscapedUnicode \\\u3044\\\u3069 User\"@NCLMailTest.com>" };
            yield return new object[] { "\"UnicodeUserName\" <\u3044.\u3069@NCLMailTest.com>" };
            yield return new object[] { "\"UnicodeDomainName\" <user@\u3044.\u3069>" };
            yield return new object[] { "\u3044 \u3069 \u3044.\u3069@\u3044.\u3069" };
        }

        public static IEnumerable<object[]> GetInvalidEmailTestData()
        {
            yield return new object[] { "test \"test\" user@mail.com" };
            yield return new object[] { "\"test\" \"test\" user@mail.com" };
            yield return new object[] { "\"test\" test user@mail.com" };
            yield return new object[] { "\"test\"test user@mail.com" };
            yield return new object[] { "(d)d(\\" };
            yield return new object[] { "@c.com" };
            yield return new object[] { "m@dhat@c.com" };
            yield return new object[] { "test[test]@test.com" };
            yield return new object[] { "...test@test.com" };
            yield return new object[] { ":TestUser3@NCLMailTest.com" };
            yield return new object[] { "\"unbalancedescaped\"quotes\" \"a\"@example.com" };
            yield return new object[] { "\"unbalanced escaped\"quotes\" \"a\"@example.com" };
            yield return new object[] { "\"\\\\\"QuoteEndedDispaly\" testsome@NCLMailTest.com" };
            yield return new object[] { "[bracketDomain]" };
            yield return new object[] { "missing@anglebracket>" };
            yield return new object[] { "emptyDotAtomDomain@" };
            yield return new object[] { "leading@.dot.com" };
            yield return new object[] { " " }; // empty
            yield return new object[] { " , \"Invalidcomma\" <username@domain>" };
            yield return new object[] { "\"user\".\"name\"@domain" };
            yield return new object[] { "\"user\".name@domain" };
            yield return new object[] { "<\"user\".\"name\"@domain>" };
            yield return new object[] { "Bob \"display\" <user@host>" };
            yield return new object[] { "testuser@[mail.com \r ]" };
            yield return new object[] { "testuser@[mail.com \n ]" };
            yield return new object[] { "testuser@[mail\u3069.com]" }; // No unicode allowed in square brackets
            yield return new object[] { "invalid@unicode\uD800.com" }; // D800 is a high surrogate
            yield return new object[] { "invalid@unicode\uD800.com" }; // D800 is a high surrogate
            yield return new object[] { "invalid\uD800@unicode.com" }; // D800 is a high surrogate
            yield return new object[] { "\uD800 invalid@unicode.com" }; // D800 is a high surrogate
        }

        [Theory]
        [MemberData(nameof(GetValidEmailTestData))]
        public void TestValidEmailAddresses(string validAddress)
        {
            MailAddress sut = new MailAddress(validAddress);
        }

        [Fact]
        public void TestLargeListOfValidEmailAddresses()
        {
            string[] data = GetValidEmailTestData().Select(d => (string)d.First()).ToArray();
            string emails = string.Join(",", data);
            IList<MailAddress> results = MailAddressParser.ParseMultipleAddresses(emails);
            Assert.Equal(data.Length, results.Count);
        }

        [Fact]
        public void TestLargeListOfValidEmails_WithInvalidEmails_ShouldThrow()
        {
            string[] data = GetInvalidEmailTestData().Select(d => (string)d.First()).ToArray();
            string emails = string.Join(",", data);

            Assert.Throws<FormatException>(() => { MailAddressParser.ParseMultipleAddresses(emails); });
        }

        [Theory]
        [MemberData(nameof(GetInvalidEmailTestData))]
        public void TestInvalidEmailAddresses(string address)
        {
            Assert.Throws<FormatException>(() => { new MailAddress(address); });
        }
    }
}
