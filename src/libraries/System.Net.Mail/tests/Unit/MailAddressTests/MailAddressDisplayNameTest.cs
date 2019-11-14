// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailAddressDisplayNameTest
    {
        private const string Address = "test@example.com";
        private const string DisplayNameWithUnicode = "DisplayNameWith\u00C9\u00C0\u0106\u0100\u0109\u0105\u00E4Unicode";
        private const string DisplayNameWithNoUnicode = "testDisplayName";

        [Fact]
        public void MailAddress_WithUnicodeDisplayAndMailAddress_ToStringShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets()
        {
            MailAddress _mailAddress = new MailAddress(Address, DisplayNameWithUnicode);
            Assert.Equal(_mailAddress.DisplayName, DisplayNameWithUnicode);

            Assert.Equal(string.Format("\"{0}\" <{1}>", DisplayNameWithUnicode, Address), _mailAddress.ToString());
        }

        [Fact]
        public void MailAddress_WithNoDisplayName_AndOnlyAddress_ToStringShouldOutputAddressOnlyWithNoAngleBrackets()
        {
            MailAddress _mailAddress = new MailAddress(Address);
            Assert.Equal(Address, _mailAddress.ToString());
        }

        [Fact]
        public void MailAddress_WithNoUnicodeDisplayAndMailAddress_ToStringShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets()
        {
            MailAddress _mailAddress = new MailAddress(Address, DisplayNameWithNoUnicode);
            Assert.Equal(_mailAddress.DisplayName, DisplayNameWithNoUnicode);

            Assert.Equal(Address, _mailAddress.Address);
            Assert.Equal(string.Format("\"{0}\" <{1}>", DisplayNameWithNoUnicode, Address), _mailAddress.ToString());
        }

        [Fact]
        public void MailAddress_WithNoUnicodeDisplayAndMailAddress_ConstructorShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets()
        {
            MailAddress _mailAddress = new MailAddress(string.Format("\"{0}\" <{1}>", DisplayNameWithNoUnicode, Address));
            Assert.Equal(_mailAddress.DisplayName, DisplayNameWithNoUnicode);

            Assert.Equal(Address, _mailAddress.Address);
            Assert.Equal(string.Format("\"{0}\" <{1}>", DisplayNameWithNoUnicode, Address), _mailAddress.ToString());
        }

        [Fact]
        public void MailAddress_WithUnicodeDisplayAndMailAddress_ConstructorShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets()
        {
            MailAddress _mailAddress = new MailAddress(string.Format("\"{0}\" <{1}>", DisplayNameWithUnicode, Address));
            Assert.Equal(DisplayNameWithUnicode, _mailAddress.DisplayName);

            Assert.Equal(Address, _mailAddress.Address);
            Assert.Equal(string.Format("\"{0}\" <{1}>", DisplayNameWithUnicode, Address), _mailAddress.ToString());
        }

        [Fact]
        public void MailAddress_WithNonQuotedUnicodeDisplayAndMailAddress_ConstructorShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets()
        {
            MailAddress _mailAddress = new MailAddress(string.Format("{0} <{1}>", DisplayNameWithUnicode, Address));
            Assert.Equal(DisplayNameWithUnicode, _mailAddress.DisplayName);

            Assert.Equal(Address, _mailAddress.Address);
            Assert.Equal(string.Format("\"{0}\" <{1}>", DisplayNameWithUnicode, Address), _mailAddress.ToString());
        }
    }
}
