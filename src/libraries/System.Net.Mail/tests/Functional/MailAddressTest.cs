// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// MailAddressTest.cs - NUnit Test Cases for System.Net.MailAddress.MailAddress
//
// Authors:
//   John Luke (john.luke@gmail.com)
//
// (C) 2005 John Luke
//

using Xunit;

namespace System.Net.Mail.Tests
{
    public class MailAddressTest
    {
        MailAddress address;

        public MailAddressTest()
        {
            address = new MailAddress("foo@example.com", "Mr. Foo Bar");
        }

        [Fact]
        public void TestConstructorOverload1()
        {
            address = new MailAddress(" foo@example.com ");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal(string.Empty, address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("foo@example.com", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("Mr. Foo Bar <foo@example.com>");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal("Mr. Foo Bar", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("\"Mr. Foo Bar\" <foo@example.com>", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("FooBar <foo@example.com>");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal("FooBar", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("\"FooBar\" <foo@example.com>", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("\"FooBar\"foo@example.com   ");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal("FooBar", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("\"FooBar\" <foo@example.com>", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("\"   FooBar   \"< foo@example.com >");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal("   FooBar   ", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("\"   FooBar   \" <foo@example.com>", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("<foo@example.com>");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal(string.Empty, address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("foo@example.com", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("    <  foo@example.com  >");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal(string.Empty, address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("foo@example.com", address.ToString());
            Assert.Equal("foo", address.User);
        }

        [Fact]
        public void TestConstructorWithNullString()
        {
            Assert.Throws<ArgumentNullException>(() => new MailAddress(null));
        }

        [Fact]
        public void TestConstructorWithEmptyString()
        {
            AssertExtensions.Throws<ArgumentException>("address", () => new MailAddress(""));
        }

        [Fact]
        public void TestInvalidAddressInConstructor()
        {
            Assert.Throws<FormatException>(() => new MailAddress("Mr. Foo Bar"));
            Assert.Throws<FormatException>(() => new MailAddress("foo@b@ar"));
            Assert.Throws<FormatException>(() => new MailAddress("Mr. Foo Bar <foo@exa<mple.com"));
            Assert.Throws<FormatException>(() => new MailAddress("Mr. Foo Bar <foo@example.com"));
            Assert.Throws<FormatException>(() => new MailAddress("Mr. \"F@@ Bar\" <foo@example.com> Whatever@You@Want"));
            Assert.Throws<FormatException>(() => new MailAddress("Mr. F@@ Bar <foo@example.com> What\"ever@You@Want"));
            Assert.Throws<FormatException>(() => new MailAddress("\"MrFo@Bar\""));
            Assert.Throws<FormatException>(() => new MailAddress("\"MrFo@Bar\"<>"));
            Assert.Throws<FormatException>(() => new MailAddress(" "));
            Assert.Throws<FormatException>(() => new MailAddress("forbar"));
            Assert.Throws<FormatException>(() => new MailAddress("<foo@example.com> WhatEver", " Mr. Foo Bar "));
            Assert.Throws<FormatException>(() => new MailAddress("Mr. Far Bar <foo@example.com> Whatever", "BarFoo"));
        }

        [Fact]
        public void TestConstructorOverload2()
        {
            address = new MailAddress(" foo@example.com ", null);
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal(string.Empty, address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("foo@example.com", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("Mr. Far Bar <foo@example.com>", "BarFoo");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal("BarFoo", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("\"BarFoo\" <foo@example.com>", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("Mr. Far Bar <foo@example.com>  ", string.Empty);
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal("Mr. Far Bar", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("\"Mr. Far Bar\" <foo@example.com>", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("Mr. Far Bar <foo@example.com>", null);
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal("Mr. Far Bar", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("\"Mr. Far Bar\" <foo@example.com>", address.ToString());
            Assert.Equal("foo", address.User);

            address = new MailAddress("Mr. Far Bar <foo@example.com>   ", " ");
            Assert.Equal("foo@example.com", address.Address);
            Assert.Equal(" ", address.DisplayName);
            Assert.Equal("example.com", address.Host);
            Assert.Equal("\" \" <foo@example.com>", address.ToString());
            Assert.Equal("foo", address.User);
        }

        [Fact]
        public void DisplayName_Precedence()
        {
            var ma = new MailAddress("Hola <foo@bar.com>");
            Assert.Equal(ma.DisplayName, "Hola");
            ma = new MailAddress("Hola <foo@bar.com>", "Adios");
            Assert.Equal(ma.DisplayName, "Adios");
            ma = new MailAddress("Hola <foo@bar.com>", "");
            Assert.Equal(ma.DisplayName, "Hola");
            ma = new MailAddress("<foo@bar.com>", "");
            Assert.Equal(ma.DisplayName, "");
        }
        
        [Fact]
        public void Address_QuoteFirst()
        {
            new MailAddress("\"Hola\" <foo@bar.com>");
        }

        [Fact]
        public void Address_QuoteNotFirst()
        {
            Assert.Throws<FormatException>(() => new MailAddress("H\"ola\" <foo@bar.com>"));
        }

        [Fact]
        public void Address_NoClosingQuote()
        {
            Assert.Throws<FormatException>(() => new MailAddress("\"Hola <foo@bar.com>"));
        }

        [Fact]
        public void Address_NoUser()
        {
            Assert.Throws<FormatException>(() => new MailAddress("Hola <@bar.com>"));
        }

        [Fact]
        public void Address_NoUserNoHost()
        {
            Assert.Throws<FormatException>(() => new MailAddress("Hola <@>"));
        }

        [Fact]
        public void Address()
        {
            Assert.Equal("foo@example.com", address.Address);
        }

        [Fact]
        public void DisplayName()
        {
            Assert.Equal("Mr. Foo Bar", address.DisplayName);
        }

        [Fact]
        public void Host()
        {
            Assert.Equal("example.com", address.Host);
        }

        [Fact]
        public void User()
        {
            Assert.Equal("foo", address.User);
        }

        [Fact]
        public void ToStringTest()
        {
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
    }
}
