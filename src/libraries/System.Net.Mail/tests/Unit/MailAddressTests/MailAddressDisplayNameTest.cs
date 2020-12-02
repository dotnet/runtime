// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [Theory]
        [InlineData(Address, "test\"Display\"Name")]
        [InlineData(Address, "Hello \"world hello\" world")]
        [InlineData(Address, "Hello \"world")]
        [InlineData(Address, "Hello \"\"world")]
        [InlineData(Address, "\"")]
        [InlineData(Address, "Hello \\\"world hello\\\" world")]
        public void MailAddress_WithDoubleQuotesDisplayAndMailAddress_ToStringShouldReturnEscapedDisplayNameAndAddressInAngleBrackets(string address, string displayName)
        {
            MailAddress mailAddress = new MailAddress(address, displayName);
            Assert.Equal(displayName, mailAddress.DisplayName);
            Assert.Equal(string.Format("\"{0}\" <{1}>", displayName.Replace("\"", "\\\""), address), mailAddress.ToString());
        }

        [Theory]
        [InlineData(Address, "\"John Doe\"")]
        [InlineData(Address, "\"\"")]
        [InlineData(Address, "\"\"\"")]
        [InlineData(Address, "\"John \"Johnny\" Doe\"")]
        public void MailAddress_WithOuterDoubleQuotesDisplayAndMailAddress_ToStringShouldReturnEscapedDisplayNameAndAddressInAngleBrackets(string address, string displayName)
        {
            MailAddress mailAddress = new MailAddress(address, displayName);
            displayName = displayName.Substring(1, displayName.Length - 2);

            Assert.Equal(displayName, mailAddress.DisplayName);

            if (string.IsNullOrEmpty(displayName))
            {
                Assert.Equal($"{address}", mailAddress.ToString());
            }
            else
            {
                Assert.Equal($"\"{displayName.Replace("\"", "\\\"")}\" <{address}>", mailAddress.ToString());
            }
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
