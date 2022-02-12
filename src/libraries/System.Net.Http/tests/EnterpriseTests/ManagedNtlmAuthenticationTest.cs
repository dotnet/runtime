// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Test.Common;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Http.Enterprise.Tests
{
    public class ManagedNtlmAuthenticationTest
    {
        [Theory]
        [InlineData("NTLM")]
        [InlineData("Negotiate")]
        public void HttpClient_ManagedNtlmValidAuthentication_Success(string authType)
        {
            using var handler = new HttpClientHandler();
            var creds = new CredentialCache();
            creds.Add(new Uri("http://emclientntlm.westus.cloudapp.azure.com/"), authType, new NetworkCredential("TESTUSER", "grundlE!12345", "emclientntlm"));
            handler.Credentials = creds;
            using var client = new HttpClient(handler);

            using HttpResponseMessage response = client.GetAsync("http://emclientntlm.westus.cloudapp.azure.com/").GetAwaiter().GetResult();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
