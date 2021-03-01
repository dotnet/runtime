// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailAddressTest
    {
        public static IEnumerable<object[]> GetValid_Address()
        {
            // inputAddress, address, displayName, host, toString(), user
            yield return new object[] { " foo@example.com ", "foo@example.com", string.Empty, "example.com", "foo@example.com", "foo" };
            yield return new object[] { "Mr. Foo Bar <foo@example.com>", "foo@example.com", "Mr. Foo Bar", "example.com", "\"Mr. Foo Bar\" <foo@example.com>", "foo" };
            yield return new object[] { "FooBar <foo@example.com>", "foo@example.com", "FooBar", "example.com", "\"FooBar\" <foo@example.com>", "foo" };
            yield return new object[] { "\"FooBar\"foo@example.com   ", "foo@example.com", "FooBar", "example.com", "\"FooBar\" <foo@example.com>", "foo" };
            yield return new object[] { "\"   FooBar   \"< foo@example.com >", "foo@example.com", "   FooBar   ", "example.com", "\"   FooBar   \" <foo@example.com>", "foo" };
            yield return new object[] { "<foo@example.com>", "foo@example.com", string.Empty, "example.com", "foo@example.com", "foo" };
            yield return new object[] { "    <  foo@example.com  >", "foo@example.com", string.Empty, "example.com", "foo@example.com", "foo" };
        }

        [Theory]
        [MemberData(nameof(GetValid_Address))]
        public void TestConstructor_Address(string inputAddress, string address, string displayName, string host, string toString, string user)
        {
            MailAddress addressInstance = new MailAddress(inputAddress);
            Assert.Equal(address, addressInstance.Address);
            Assert.Equal(displayName, addressInstance.DisplayName);
            Assert.Equal(host, addressInstance.Host);
            Assert.Equal(toString, addressInstance.ToString());
            Assert.Equal(user, addressInstance.User);
        }

        [Fact]
        public void TestConstructorWithNullString()
        {
            Assert.Throws<ArgumentNullException>(() => new MailAddress(null));
            Assert.False(MailAddress.TryCreate(null, out MailAddress address));
            Assert.Null(address);
        }

        [Fact]
        public void TestConstructorWithEmptyString()
        {
            AssertExtensions.Throws<ArgumentException>("address", () => new MailAddress(""));
            Assert.False(MailAddress.TryCreate("", out MailAddress address));
            Assert.Null(address);
        }

        public static IEnumerable<object[]> GetInvalid_Address()
        {
            yield return new object[] { "Mr. Foo Bar" };
            yield return new object[] { "foo@b@ar" };
            yield return new object[] { "Mr. Foo Bar <foo@exa<mple.com" };
            yield return new object[] { "Mr. Foo Bar <foo@example.com" };
            yield return new object[] { "Mr. \"F@@ Bar\" <foo@example.com> Whatever@You@Want" };
            yield return new object[] { "Mr. F@@ Bar <foo@example.com> What\"ever@You@Want" };
            yield return new object[] { "\"MrFo@Bar\"" };
            yield return new object[] { "\"MrFo@Bar\"<>" };
            yield return new object[] { " " };
            yield return new object[] { "forbar" };
            yield return new object[] { "" };
            yield return new object[] { null };
        }

        [Theory]
        [MemberData(nameof(GetInvalid_Address))]
        public void TestInvalidAddressInConstructor_Address(string invalidAddress)
        {
            Action act = () => new MailAddress(invalidAddress);
            if (invalidAddress is null)
            {
                Assert.Throws<ArgumentNullException>(act);
            }
            else if (invalidAddress == string.Empty)
            {
                Assert.Throws<ArgumentException>(act);
            }
            else
            {
                Assert.Throws<FormatException>(act);
            }
        }

        public static IEnumerable<object[]> GetInvalid_AddressDisplayName()
        {
            yield return new object[] { "<foo@example.com> WhatEver", " Mr. Foo Bar " };
            yield return new object[] { "Mr. Far Bar <foo@example.com> Whatever", "BarFoo" };
        }

        [Theory]
        [MemberData(nameof(GetInvalid_AddressDisplayName))]
        public void TestInvalidAddressInConstructor_AddressDisplayName(string invalidAddress, string displayName)
        {
            Assert.Throws<FormatException>(() => new MailAddress(invalidAddress, displayName));
        }

        public static IEnumerable<object[]> GetValid_AddressDisplayName()
        {
            // inputAddress, inputDisplayName, address, displayName, host, toString(), user
            yield return new object[] { " foo@example.com ", null, "foo@example.com", string.Empty, "example.com", "foo@example.com", "foo" };
            yield return new object[] { "Mr. Far Bar <foo@example.com>", "BarFoo", "foo@example.com", "BarFoo", "example.com", "\"BarFoo\" <foo@example.com>", "foo" };
            yield return new object[] { "Mr. Far Bar <foo@example.com>  ", string.Empty, "foo@example.com", "Mr. Far Bar", "example.com", "\"Mr. Far Bar\" <foo@example.com>", "foo" };
            yield return new object[] { "Mr. Far Bar <foo@example.com>", null, "foo@example.com", "Mr. Far Bar", "example.com", "\"Mr. Far Bar\" <foo@example.com>", "foo" };
            yield return new object[] { "Mr. Far Bar <foo@example.com>   ", " ", "foo@example.com", " ", "example.com", "\" \" <foo@example.com>", "foo" };
        }

        [Theory]
        [MemberData(nameof(GetValid_AddressDisplayName))]
        public void TestConstructor_AddressDisplayName(string inputAddress, string inputDisplayName, string address, string displayName, string host, string toString, string user)
        {
            MailAddress addressInstance = new MailAddress(inputAddress, inputDisplayName);
            Assert.Equal(address, addressInstance.Address);
            Assert.Equal(displayName, addressInstance.DisplayName);
            Assert.Equal(host, addressInstance.Host);
            Assert.Equal(toString, addressInstance.ToString());
            Assert.Equal(user, addressInstance.User);
        }

        public static IEnumerable<object[]> GetAddress_DisplayNamePrecedence()
        {
            // inputAddress, displayName
            yield return new object[] { "Hola <foo@bar.com>", "Hola" };
        }

        [Theory]
        [MemberData(nameof(GetAddress_DisplayNamePrecedence))]
        public void DisplayName_Precedence(string inputAddress, string displayName)
        {
            var ma = new MailAddress(inputAddress);
            Assert.Equal(displayName, ma.DisplayName);
        }

        public static IEnumerable<object[]> GetAddressDisplayName_DisplayNamePrecedence()
        {
            // inputAddress, inputDisplayName, displayName
            yield return new object[] { "Hola <foo@bar.com>", "Adios", "Adios" };
            yield return new object[] { "Hola <foo@bar.com>", "", "Hola" };
            yield return new object[] { "<foo@bar.com>", "", "" };
        }

        [Theory]
        [MemberData(nameof(GetAddressDisplayName_DisplayNamePrecedence))]
        public void AddressDisplayName_Precedence(string inputAddress, string inputDisplayName, string displayName)
        {
            var ma = new MailAddress(inputAddress, inputDisplayName);
            Assert.Equal(displayName, ma.DisplayName);
        }

        [Fact]
        public void Address_QuoteFirst()
        {
            new MailAddress("\"Hola\" <foo@bar.com>");
            Assert.True(MailAddress.TryCreate("\"Hola\" <foo@bar.com>", out MailAddress _));
        }

        [Fact]
        public void Address_QuoteNotFirst()
        {
            Assert.Throws<FormatException>(() => new MailAddress("H\"ola\" <foo@bar.com>"));
            Assert.False(MailAddress.TryCreate("H\"ola\" <foo@bar.com>", out MailAddress _));
        }

        [Fact]
        public void Address_NoClosingQuote()
        {
            Assert.Throws<FormatException>(() => new MailAddress("\"Hola <foo@bar.com>"));
            Assert.False(MailAddress.TryCreate("\"Hola <foo@bar.com>", out MailAddress _));
        }

        [Fact]
        public void Address_NoUser()
        {
            Assert.Throws<FormatException>(() => new MailAddress("Hola <@bar.com>"));
            Assert.False(MailAddress.TryCreate("Hola <@bar.com>", out MailAddress _));
        }

        [Fact]
        public void Address_NoUserNoHost()
        {
            Assert.Throws<FormatException>(() => new MailAddress("Hola <@>"));
            Assert.False(MailAddress.TryCreate("Hola <@>", out MailAddress _));
        }

        [Fact]
        public void MailAddress_AllMembers()
        {
            MailAddress address = new MailAddress("foo@example.com", "Mr. Foo Bar");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal("Mr. Foo Bar", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("foo", address.User);
            Assert.Equal("\"Mr. Foo Bar\" <foo@example.com>", address.ToString());
        }

        [Fact]
        public void EqualsTest()
        {
            var n = new MailAddress("Mr. Bar <a@example.com>");
            var n2 = new MailAddress("a@example.com", "Mr. Bar");
            Assert.Equal(n, n2);
        }

        [Fact]
        public void EqualsTest2()
        {
            var n = new MailAddress("Mr. Bar <a@example.com>");
            var n2 = new MailAddress("MR. BAR <a@EXAMPLE.com>");
            Assert.Equal(n, n2);
        }

        [Fact]
        public void GetHashCodeTest()
        {
            var n = new MailAddress("Mr. Bar <a@example.com>");
            var n2 = new MailAddress("a@example.com", "Mr. Bar");
            Assert.Equal(n.GetHashCode(), n2.GetHashCode());
        }

        [Fact]
        public void GetHashCodeTest2()
        {
            var n = new MailAddress("Mr. Bar <a@example.com>");
            var n2 = new MailAddress("MR. BAR <a@EXAMPLE.com>");
            Assert.Equal(n.GetHashCode(), n2.GetHashCode());
        }

        [Theory]
        [MemberData(nameof(GetInvalid_Address))]
        public void TryCreate_Invalid_Address(string address)
        {
            Assert.False(MailAddress.TryCreate(address, out MailAddress _));
        }

        [Theory]
        [MemberData(nameof(GetInvalid_AddressDisplayName))]
        public void TryCreate_Invalid_AddressAndDisplayName(string address, string displayName)
        {
            Assert.False(MailAddress.TryCreate(address, displayName, out MailAddress _));
        }

        [Theory]
        [MemberData(nameof(GetValid_Address))]
        public void TryCreate_Valid_Address(string inputAddress, string address, string displayName, string host, string toString, string user)
        {
            Assert.True(MailAddress.TryCreate(inputAddress, out MailAddress addressInstance));
            Assert.Equal(address, addressInstance.Address);
            Assert.Equal(displayName, addressInstance.DisplayName);
            Assert.Equal(host, addressInstance.Host);
            Assert.Equal(toString, addressInstance.ToString());
            Assert.Equal(user, addressInstance.User);
        }

        [Theory]
        [MemberData(nameof(GetValid_AddressDisplayName))]
        public void TryCreate_Valid_AddressDisplayName(string inputAddress, string inputDisplayName, string address, string displayName, string host, string toString, string user)
        {
            Assert.True(MailAddress.TryCreate(inputAddress, inputDisplayName, out MailAddress addressInstance));
            Assert.Equal(address, addressInstance.Address);
            Assert.Equal(displayName, addressInstance.DisplayName);
            Assert.Equal(host, addressInstance.Host);
            Assert.Equal(toString, addressInstance.ToString());
            Assert.Equal(user, addressInstance.User);
        }

        [Theory]
        [MemberData(nameof(GetAddress_DisplayNamePrecedence))]
        public void TryCreate_DisplayName_Precedence(string inputAddress, string displayName)
        {
            Assert.True(MailAddress.TryCreate(inputAddress, out MailAddress addressInstance));
            Assert.Equal(displayName, addressInstance.DisplayName);
        }

        [Theory]
        [MemberData(nameof(GetAddressDisplayName_DisplayNamePrecedence))]
        public void TryCreate_AddressDisplayName_Precedence(string inputAddress, string inputDisplayName, string displayName)
        {
            Assert.True(MailAddress.TryCreate(inputAddress, inputDisplayName, out MailAddress addressInstance));
            Assert.Equal(displayName, addressInstance.DisplayName);
        }
    }
}
