// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailAddressDisplayNameTest
    {
        const string Address = "test@example.com";
        const string DisplayName = "Display Name";
        const string UnicodeDisplayName = "Display \u00C9\u00C0\u0106\u0100\u0109\u0105\u00E4 Name";

        public static IEnumerable<object[]> MailAddressTestData()
        {
            yield return new object[]{ Address, DisplayName, null, $"\"{DisplayName}\" <{Address}>" };
            yield return new object[]{ Address, UnicodeDisplayName, null, $"\"{UnicodeDisplayName}\" <{Address}>" };
            yield return new object[]{ Address, $"\"{DisplayName}\"", DisplayName, $"\"{DisplayName}\" <{Address}>" };
            yield return new object[]{ Address, $"\"{UnicodeDisplayName}\"", UnicodeDisplayName, $"\"{UnicodeDisplayName}\" <{Address}>" };
        }
        public static IEnumerable<object[]> MailAddressTestDataQuotes()
        {
            var displayNamesWithQuotes = new[]
            {
                "Display \" Name",
                "Display \"\" Name",
                "Display \"Test\" Name",
                "Display \\\"Test\\\" Name",
                "\"",
            };
            foreach (var displayName in displayNamesWithQuotes)
            {
                yield return new object[]{ Address, displayName, null, $"\"{displayName.Replace("\"", "\\\"")}\" <{Address}>" };
                yield return new object[]{ Address, $"\"{displayName}\"", displayName, $"\"{displayName.Replace("\"", "\\\"")}\" <{Address}>" };
            }

            yield return new object[]{ Address, null, "", Address };
            yield return new object[]{ Address, "\"\"", "", Address };
        }

        [Theory]
        [MemberData(nameof(MailAddressTestData))]
        [MemberData(nameof(MailAddressTestDataQuotes))]
        public void MailAddress_Ctor_Succeeds(string address, string displayName, string expectedDisplayName, string expectedToString)
        {
            var mailAddress = new MailAddress(address, displayName);

            Assert.Equal(address, mailAddress.Address);
            Assert.Equal(expectedDisplayName ?? displayName, mailAddress.DisplayName);
            Assert.Equal(expectedToString, mailAddress.ToString());
        }

        [Theory]
        [MemberData(nameof(MailAddressTestData))]
        public void MailAddress_Ctor_FullString_Succeeds(string address, string displayName, string expectedDisplayName, string expectedToString)
        {
            var mailAddress = new MailAddress($"{displayName} <{Address}>");

            Assert.Equal(address, mailAddress.Address);
            Assert.Equal(expectedDisplayName ?? displayName, mailAddress.DisplayName);
            Assert.Equal(expectedToString, mailAddress.ToString());
        }
    }
}
