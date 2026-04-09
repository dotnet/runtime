// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Mail.Functional.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "SmtpClient is not supported on Browser")]
    public class SmtpClientCredentialsTest
    {
        private readonly string UserName = "user";
        private readonly string Password = Guid.NewGuid().ToString();

        [Fact]
        public void Credentials_Unset_Null()
        {
            SmtpClient client = new SmtpClient();

            ICredentialsByHost transportCredentials = GetTransportCredentials(client);

            Assert.Null(client.Credentials);
            Assert.Null(transportCredentials);
        }

        [Fact]
        public void DefaultCredentials_True_DefaultCredentials()
        {
            NetworkCredential expectedCredentials = CredentialCache.DefaultNetworkCredentials;

            SmtpClient client = new SmtpClient();
            client.UseDefaultCredentials = true;

            ICredentialsByHost transportCredentials = GetTransportCredentials(client);

            Assert.Equal(expectedCredentials, client.Credentials);
            Assert.Equal(expectedCredentials, transportCredentials);
        }

        [Fact]
        public void UseDefaultCredentials_False_Null()
        {
            SmtpClient client = new SmtpClient();
            client.UseDefaultCredentials = false;

            ICredentialsByHost transportCredentials = GetTransportCredentials(client);

            Assert.Null(client.Credentials);
            Assert.Null(transportCredentials);
        }

        [Fact]
        public void Credentials_UseDefaultCredentialsSetFalseBeforeCredentials_Credentials()
        {
            NetworkCredential expectedCredentials = new NetworkCredential(UserName, Password);

            SmtpClient client = new SmtpClient();
            client.UseDefaultCredentials = false;
            client.Credentials = expectedCredentials;

            ICredentialsByHost transportCredentials = GetTransportCredentials(client);

            Assert.NotNull(client.Credentials);
            Assert.Equal(expectedCredentials, client.Credentials);
            Assert.Equal(expectedCredentials, transportCredentials);
        }

        [Fact]
        public void Credentials_UseDefaultCredentialsSetFalseAfterCredentials_Credentials()
        {
            NetworkCredential expectedCredentials = new NetworkCredential(UserName, Password);

            SmtpClient client = new SmtpClient();
            client.Credentials = expectedCredentials;
            client.UseDefaultCredentials = false;

            ICredentialsByHost transportCredentials = GetTransportCredentials(client);

            Assert.NotNull(client.Credentials);
            Assert.Equal(expectedCredentials, client.Credentials);
            Assert.Equal(expectedCredentials, transportCredentials);
        }

        [Fact]
        public void Credentials_UseDefaultCredentialsSetTrueBeforeCredentials_DefaultNetworkCredentials()
        {
            NetworkCredential expectedCredentials = CredentialCache.DefaultNetworkCredentials;

            SmtpClient client = new SmtpClient();
            client.UseDefaultCredentials = true;
            client.Credentials = new NetworkCredential(UserName, Password);

            ICredentialsByHost transportCredentials = GetTransportCredentials(client);

            Assert.Equal(expectedCredentials, client.Credentials);
            Assert.Equal(expectedCredentials, transportCredentials);
        }

        [Fact]
        public void Credentials_UseDefaultCredentialsSetTrueAfterCredentials_DefaultNetworkCredentials()
        {
            NetworkCredential expectedCredentials = CredentialCache.DefaultNetworkCredentials;

            SmtpClient client = new SmtpClient();
            client.Credentials = new NetworkCredential(UserName, Password);
            client.UseDefaultCredentials = true;

            ICredentialsByHost transportCredentials = GetTransportCredentials(client);

            Assert.Equal(expectedCredentials, client.Credentials);
            Assert.Equal(expectedCredentials, transportCredentials);
        }

        private ICredentialsByHost GetTransportCredentials(SmtpClient client)
        {
            Type smtpTransportType = Type.GetType("System.Net.Mail.SmtpTransport, System.Net.Mail");

            var transport = typeof(SmtpClient)
                .GetField("_transport", BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance)
                .GetValue(client);

            var transportCredentials = smtpTransportType
                .GetProperty("Credentials", BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance)
                .GetValue(transport);

            return (ICredentialsByHost)transportCredentials;
        }
    }
}
