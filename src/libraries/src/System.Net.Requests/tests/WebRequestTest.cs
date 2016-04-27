// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Tests
{
    public class WebRequestTest
    {
        private readonly NetworkCredential _explicitCredentials = new NetworkCredential("user", "password", "domain");

        [Fact]
        public void DefaultWebProxy_VerifyDefaults_Success()
        {
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            Assert.NotNull(proxy);

            // Since WebRequest.DefaultWebProxy is a static property, the initial default value for
            // Credentials is only null iff. no other test method in the test process has changed
            // the value in a prior test.
            Assert.Null(proxy.Credentials);
        }

        [Fact]
        public void DefaultWebProxy_SetCredentialsToNullThenGet_ValuesMatch()
        {
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            proxy.Credentials = null;
            Assert.Equal(null, proxy.Credentials);
        }

        [Fact]
        public void DefaultWebProxy_SetCredentialsToDefaultCredentialsThenGet_ValuesMatch()
        {
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            proxy.Credentials = CredentialCache.DefaultCredentials;
            Assert.Equal(CredentialCache.DefaultCredentials, proxy.Credentials);
        }

        [Fact]
        public void DefaultWebProxy_SetCredentialsToExplicitCredentialsThenGet_ValuesMatch()
        {
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            ICredentials oldCreds = proxy.Credentials;
            try
            {
                proxy.Credentials = _explicitCredentials;
                Assert.Equal(_explicitCredentials, proxy.Credentials);
            }
            finally
            {
                // Reset the credentials so as not to interfere with any subsequent tests, 
                // e.g. DefaultWebProxy_VerifyDefaults_Success
                proxy.Credentials = oldCreds;
            }
        }

        [Theory]
        [InlineData("http")]
        [InlineData("https")]
        public void Create_ValidWebRequestUriScheme_Success(string scheme)
        {
            var uri = new Uri($"{scheme}://example.com/folder/resource.txt");
            WebRequest request = WebRequest.Create(uri);
        }

        [Theory]
        [InlineData("ws")]
        [InlineData("wss")]
        [InlineData("file")]
        [InlineData("ftp")]
        [InlineData("custom")]
        public void Create_InvalidWebRequestUriScheme_Throws(string scheme)
        {
            var uri = new Uri($"{scheme}://example.com/folder/resource.txt");
            Assert.Throws<NotSupportedException>(() => WebRequest.Create(uri));
        }        
    }
}
