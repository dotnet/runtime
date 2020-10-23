// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailAddressDisplayNameTest
    {
        private const string Address = "test@example.com";
        private const string DisplayNameWithUnicode = "DisplayNameWith\u00C9\u00C0\u0106\u0100\u0109\u0105\u00E4Unicode";
        private const string DisplayNameWithNoUnicode = "testDisplayName";

        [Theory]
        [InlineData(DisplayNameWithUnicode)]
        [InlineData(DisplayNameWithNoUnicode)]
        public void MailAddress_WithDisplayNameAndMailAddress_ToStringShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets(string displayName)
        {
            var mailAddress = new MailAddress(Address, displayName);

            Assert.Equal(displayName, mailAddress.DisplayName);
            Assert.Equal(Address, mailAddress.Address);
            Assert.Equal($"\"{displayName}\" <{Address}>", mailAddress.ToString());
        }

        [Theory]
        [InlineData("test\"Display\"Name")]
        [InlineData("Hello \"world hello\" world")]
        [InlineData("Hello \"world")]
        [InlineData("Hello \"\"world")]
        [InlineData("\"")]
        [InlineData("Hello \\\"world hello\\\" world")]
        public void MailAddress_WithDoubleQuotesDisplayAndMailAddress_ToStringShouldReturnEscapedDisplayNameAndAddressInAngleBrackets(string displayName)
        {
            MailAddress mailAddress = new MailAddress(Address, displayName);
            Assert.Equal(displayName, mailAddress.DisplayName);
            Assert.Equal(string.Format("\"{0}\" <{1}>", displayName.Replace("\"", "\\\""), Address), mailAddress.ToString());
        }

        [Theory]
        [InlineData("\"John Doe\"")]
        [InlineData("\"\"")]
        [InlineData("\"\"\"")]
        [InlineData("\"John \"Johnny\" Doe\"")]
        public void MailAddress_WithOuterDoubleQuotesDisplayAndMailAddress_ToStringShouldReturnEscapedDisplayNameAndAddressInAngleBrackets(string displayName)
        {
            MailAddress mailAddress = new MailAddress(Address, displayName);
            displayName = displayName.Substring(1, displayName.Length - 2);

            Assert.Equal(displayName, mailAddress.DisplayName);

            if (string.IsNullOrEmpty(displayName))
            {
                Assert.Equal($"{Address}", mailAddress.ToString());
            }
            else
            {
                Assert.Equal($"\"{displayName.Replace("\"", "\\\"")}\" <{Address}>", mailAddress.ToString());
            }
        }


        [Fact]
        public void MailAddress_WithAddressOnly_ToStringShouldOutputAddressOnlyWithNoAngleBrackets()
        {
            var mailAddress = new MailAddress(Address);
            Assert.Equal(Address, mailAddress.ToString());
        }

        [Theory]
        [InlineData(DisplayNameWithNoUnicode, false)]
        [InlineData(DisplayNameWithNoUnicode, true)]
        [InlineData(DisplayNameWithUnicode, false)]
        [InlineData(DisplayNameWithUnicode, true)]
        public void MailAddress_WithDisplayNameAndMailAddress_ToStringShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets(string displayName, bool addQuotes)
        {
            var mailAddress = new MailAddress($"{(addQuotes ? "\"" + displayName + "\"" : displayName)} <{Address}>");
            Assert.Equal(displayName, mailAddress.DisplayName);
            Assert.Equal(Address, mailAddress.Address);
            Assert.Equal($"\"{displayName}\" <{Address}>", mailAddress.ToString());
        }
    }
}
