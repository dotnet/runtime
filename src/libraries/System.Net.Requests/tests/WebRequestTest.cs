// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Tests
{
    public class WebRequestTest
    {
        static WebRequestTest()
        {
            // Capture the value of DefaultWebProxy before any tests run.
            // This lets us test the default value without imposing test
            // ordering constraints which aren't natively supported by xunit.
            initialDefaultWebProxy = WebRequest.DefaultWebProxy;
            initialDefaultWebProxyCredentials = initialDefaultWebProxy.Credentials;
        }

        private readonly NetworkCredential _explicitCredentials = new NetworkCredential("user", "password", "domain");
        private static IWebProxy initialDefaultWebProxy;
        private static ICredentials initialDefaultWebProxyCredentials;

        [Fact]
        public void DefaultWebProxy_VerifyDefaults_Success()
        {
            Assert.NotNull(initialDefaultWebProxy);

            Assert.Null(initialDefaultWebProxyCredentials);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void DefaultWebProxy_SetThenGet_ValuesMatch()
        {
            RemoteExecutor.Invoke(() =>
            {
                IWebProxy p = new WebProxy();

                WebRequest.DefaultWebProxy = p;
                Assert.Same(p, WebRequest.DefaultWebProxy);
            }).Dispose();
        }

        [Fact]
        public void DefaultWebProxy_SetCredentialsToNullThenGet_ValuesMatch()
        {
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            proxy.Credentials = null;
            Assert.Null(proxy.Credentials);
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
        [InlineData("ftp")]
        public void Create_ValidWebRequestUriScheme_Success(string scheme)
        {
            var uri = new Uri($"{scheme}://example.com/folder/resource.txt");
            WebRequest.Create(uri);
        }

        [Theory]
        [InlineData("ws")]
        [InlineData("wss")]
        [InlineData("custom")]
        public void Create_InvalidWebRequestUriScheme_Throws(string scheme)
        {
            var uri = new Uri($"{scheme}://example.com/folder/resource.txt");
            Assert.Throws<NotSupportedException>(() => WebRequest.Create(uri));
        }

        [Fact]
        public void Create_NullRequestUri_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => WebRequest.Create((string)null));
            Assert.Throws<ArgumentNullException>(() => WebRequest.Create((Uri)null));
        }

        [Fact]
        public void CreateDefault_NullRequestUri_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => WebRequest.CreateDefault(null));
        }

        [Fact]
        public void CreateHttp_NullRequestUri_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => WebRequest.CreateHttp((string)null));
            Assert.Throws<ArgumentNullException>(() => WebRequest.CreateHttp((Uri)null));
        }

        [Fact]
        public void CreateHttp_InvalidScheme_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => WebRequest.CreateHttp(new Uri("ftp://microsoft.com")));
        }

        [Fact]
        public void BaseMembers_NotCall_ThrowsNotImplementedException()
        {
            WebRequest request = new FakeRequest();
            Assert.Throws<NotImplementedException>(() => request.ConnectionGroupName);
            Assert.Throws<NotImplementedException>(() => request.ConnectionGroupName = null);
            Assert.Throws<NotImplementedException>(() => request.Method);
            Assert.Throws<NotImplementedException>(() => request.Method = null);
            Assert.Throws<NotImplementedException>(() => request.RequestUri);
            Assert.Throws<NotImplementedException>(() => request.Headers);
            Assert.Throws<NotImplementedException>(() => request.Headers = null);
            Assert.Throws<NotImplementedException>(() => request.ContentLength);
            Assert.Throws<NotImplementedException>(() => request.ContentLength = 0);
            Assert.Throws<NotImplementedException>(() => request.ContentType);
            Assert.Throws<NotImplementedException>(() => request.ContentType = null);
            Assert.Throws<NotImplementedException>(() => request.Credentials);
            Assert.Throws<NotImplementedException>(() => request.Credentials = null);
            Assert.Throws<NotImplementedException>(() => request.Timeout);
            Assert.Throws<NotImplementedException>(() => request.Timeout = 0);
            Assert.Throws<NotImplementedException>(() => request.UseDefaultCredentials);
            Assert.Throws<NotImplementedException>(() => request.UseDefaultCredentials = true);
            Assert.Throws<NotImplementedException>(() => request.GetRequestStream());
            Assert.Throws<NotImplementedException>(() => request.GetResponse());
            Assert.Throws<NotImplementedException>(() => request.BeginGetResponse(null, null));
            Assert.Throws<NotImplementedException>(() => request.EndGetResponse(null));
            Assert.Throws<NotImplementedException>(() => request.BeginGetRequestStream(null, null));
            Assert.Throws<NotImplementedException>(() => request.EndGetRequestStream(null));
            Assert.Throws<NotImplementedException>(() => request.Abort());
            Assert.Throws<NotImplementedException>(() => request.PreAuthenticate);
            Assert.Throws<NotImplementedException>(() => request.PreAuthenticate = true);
            Assert.Throws<NotImplementedException>(() => request.Proxy);
            Assert.Throws<NotImplementedException>(() => request.Proxy = null);
        }

        [Fact]
        public void GetSystemWebProxy_NoArguments_ExpectNotNull()
        {
            IWebProxy webProxy = WebRequest.GetSystemWebProxy();
            Assert.NotNull(webProxy);
        }

        [Fact]
        public void RegisterPrefix_PrefixOrCreatorNull_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => WebRequest.RegisterPrefix(null, new FakeRequestFactory()));
            Assert.Throws<ArgumentNullException>(() => WebRequest.RegisterPrefix("http://", null));
        }

        [Fact]
        public void RegisterPrefix_HttpWithFakeFactory_Success()
        {
            bool success = WebRequest.RegisterPrefix("sta:", new FakeRequestFactory());
            Assert.True(success);
            Assert.IsType<FakeRequest>(WebRequest.Create("sta://anything"));
        }

        [Fact]
        public void RegisterPrefix_DuplicateHttpWithFakeFactory_ExpectFalse()
        {
            bool success = WebRequest.RegisterPrefix("stb:", new FakeRequestFactory());
            Assert.True(success);
            success = WebRequest.RegisterPrefix("stb:", new FakeRequestFactory());
            Assert.False(success);
        }

        private class FakeRequest : WebRequest
        {
            private readonly Uri _uri;
            public override Uri RequestUri => _uri ?? base.RequestUri;

            public FakeRequest(Uri uri = null)
            {
                _uri = uri;
            }
        }

        private class FakeRequestFactory : IWebRequestCreate
        {
            public WebRequest Create(Uri uri)
            {
                return new FakeRequest(uri);
            }
        }

    }
}
