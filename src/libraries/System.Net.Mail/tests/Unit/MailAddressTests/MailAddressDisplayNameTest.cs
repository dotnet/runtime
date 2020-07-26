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
        [InlineData(Address, DisplayNameWithUnicode)]
        [InlineData(Address, DisplayNameWithNoUnicode)]
        public void MailAddress_WithDisplayNameAndMailAddress_ToStringShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets(string address, string displayName)
        {
            var mailAddress = new MailAddress(address, displayName);

            Assert.Equal(mailAddress.DisplayName, displayName);
            Assert.Equal(address, mailAddress.Address);
            Assert.Equal($"\"{displayName}\" <{address}>", mailAddress.ToString());
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
        public void MailAddress_WithAddressOnly_ToStringShouldOutputAddressOnlyWithNoAngleBrackets()
        {
            var mailAddress = new MailAddress(Address);
            Assert.Equal(Address, mailAddress.ToString());
        }

        private static IEnumerable<object[]> GetMailAddressDisplayNames()
        {
            yield return new object[] { Address, DisplayNameWithNoUnicode, $"\"{DisplayNameWithNoUnicode}\" <{Address}>" };
            yield return new object[] { Address, DisplayNameWithUnicode, $"\"{DisplayNameWithUnicode}\" <{Address}>" };
            yield return new object[] { Address, DisplayNameWithNoUnicode, $"{DisplayNameWithNoUnicode} <{Address}>" };
            yield return new object[] { Address, DisplayNameWithUnicode, $"{DisplayNameWithUnicode} <{Address}>" };
        }

        [Theory]
        [MemberData(nameof(GetMailAddressDisplayNames))]
        public void MailAddress_WithDisplayNamAndMailAddress_ToStringShouldReturnDisplayNameInQuotesAndAddressInAngleBrackets(string address, string displayName, string displayAndAddress)
        {
            var mailAddress = new MailAddress(displayAndAddress);
            Assert.Equal(displayName, mailAddress.DisplayName);

            Assert.Equal(address, mailAddress.Address);
            Assert.Equal($"\"{displayName}\" <{address}>", mailAddress.ToString());
        }
    }
}
