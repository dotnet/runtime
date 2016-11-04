// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Mail.Tests
{
    public class EhloParseExtensionsTest
    {
        private SmtpConnection _smtpConnection;
        private const string authEqualsLogin = "AUTH=LOGIN";
        private const string authAllTypes = "AUTH LOGIN NTLM WDIGEST GSSAPI";
        private const string authLoginOnly = "AUTH LOGIN";
        private string[] _extensions;

        private static readonly ISmtpAuthenticationModule s_loginModule = new SmtpLoginAuthenticationModule();
        private static readonly ISmtpAuthenticationModule[] s_authenticationModules = { s_loginModule };

        public EhloParseExtensionsTest()
        {
            _smtpConnection = new SmtpConnection(s_authenticationModules);
        }

        [Fact]
        public void ParseExtensions_WithNoAuthSpecified_ShouldNotSupportAnyAuthentcation()
        {
            _extensions = new string[1];
            _smtpConnection.ParseExtensions(_extensions);

            Assert.False(_smtpConnection.DSNEnabled);
            Assert.False(_smtpConnection.AuthSupported(s_loginModule));
        }

        [Fact]
        public void ParseExtensions_WithOnlyAuth_ShouldSupportAllAuthTypesAdvertised()
        {
            _extensions = new string[] { authAllTypes };
            _smtpConnection.ParseExtensions(_extensions);

            Assert.False(_smtpConnection.DSNEnabled);
            Assert.True(_smtpConnection.AuthSupported(s_loginModule));
        }

        [Fact]
        public void ParseExtensions_WithOnlyAuthEqualsLogin_ShouldSupportAuthLogin()
        {
            _extensions = new string[] { authEqualsLogin };
            _smtpConnection.ParseExtensions(_extensions);

            Assert.False(_smtpConnection.DSNEnabled);
            Assert.True(_smtpConnection.AuthSupported(s_loginModule));
        }

        [Fact]
        public void ParseExtensions_WithBothAuthAndAuthEqualsLogin_ShouldTakeSettingsFromAuth()
        {
            _extensions = new string[] { authAllTypes, authEqualsLogin };
            _smtpConnection.ParseExtensions(_extensions);

            Assert.False(_smtpConnection.DSNEnabled);
            Assert.True(_smtpConnection.AuthSupported(s_loginModule));
        }

        [Fact]
        public void ParseExtensions_WithBothAuthEqualsLoginAndAuth_ShouldTakeSettingsFromAuth()
        {
            // reverse the order that the strings occur in from the other test since we don't
            // know for sure which string will come first, although typically it's AUTH followed by AUTH=
            _extensions = new string[] { authEqualsLogin, authAllTypes };
            _smtpConnection.ParseExtensions(_extensions);

            Assert.False(_smtpConnection.DSNEnabled);
            Assert.True(_smtpConnection.AuthSupported(s_loginModule));
        }

        [Fact]
        public void ParseExtensions_WithBothAuthTypes_AndExtraExtensions_AuthTypesShouldBeCorrect()
        {
            // add extra valid EHLO responses as noise- it should ignore them
            _extensions = new string[] { authEqualsLogin, authAllTypes, "8BITMIME", "EXPN", "HELP" };
            _smtpConnection.ParseExtensions(_extensions);

            Assert.False(_smtpConnection.DSNEnabled);
            Assert.True(_smtpConnection.AuthSupported(s_loginModule));
        }

        [Fact]
        public void ParseExtensions_WithNoMechanismsAdvertised_ShouldNotSupportAnyAuthTypes()
        {
            // don't include any modules- it should still parse but no 
            // auth should be supported            
            _extensions = new string[] { "AUTH", "dsn ", "Starttls", "smtputf8" };
            _smtpConnection.ParseExtensions(_extensions);

            Assert.True(_smtpConnection.DSNEnabled);
            Assert.True(_smtpConnection.ServerSupportsEai);
            Assert.True(_smtpConnection.ServerSupportsStartTls);
            Assert.False(_smtpConnection.AuthSupported(s_loginModule));
        }

        [Fact]
        public void ParseExtensions_WithRandomGarbage_ShouldNotFail_AndShouldNotSupportAnyAuthTypes()
        {
            _extensions = new string[]
            {"stuff", "cr", "", "asdfasoihd", "14239347", "AUTH=1234 4567",
                "AUTH 3248 garbage", "AUTH  ", "AUTH= ", " ", };

            _smtpConnection.ParseExtensions(_extensions);

            Assert.False(_smtpConnection.DSNEnabled);
            Assert.False(_smtpConnection.AuthSupported(s_loginModule));
        }

        [Fact]
        public void ParseExtensions_WithOnlyAuthLogin_AuthTypesShouldBeCorrect()
        {
            _extensions = new string[] { authLoginOnly };
            _smtpConnection.ParseExtensions(_extensions);

            Assert.False(_smtpConnection.DSNEnabled);
            Assert.True(_smtpConnection.AuthSupported(s_loginModule));
        }
    }
}
