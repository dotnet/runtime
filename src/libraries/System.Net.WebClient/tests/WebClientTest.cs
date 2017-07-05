// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net.Cache;
using System.Net.Test.Common;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Tests
{
    public class WebClientTest
    {
        [Fact]
        public static void DefaultCtor_PropertiesReturnExpectedValues()
        {
            var wc = new WebClient();

            Assert.Empty(wc.BaseAddress);
            Assert.Null(wc.CachePolicy);
            Assert.Null(wc.Credentials);
            Assert.Equal(Encoding.Default, wc.Encoding);
            Assert.Empty(wc.Headers);
            Assert.False(wc.IsBusy);
            Assert.NotNull(wc.Proxy);
            Assert.Empty(wc.QueryString);
            Assert.False(wc.UseDefaultCredentials);
        }

        [Fact]
        public static void Properties_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();

            AssertExtensions.Throws<ArgumentException>("value", () => { wc.BaseAddress = "http::/invalid url"; });
            AssertExtensions.Throws<ArgumentNullException>("Encoding", () => { wc.Encoding = null; });
        }

        [Fact]
        public static void DownloadData_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadData((string)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadData((Uri)null); });

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadDataAsync((Uri)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadDataAsync((Uri)null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.DownloadDataTaskAsync((string)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadDataTaskAsync((Uri)null); });
        }

        [Fact]
        public static void DownloadFile_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadFile((string)null, ""); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadFile((Uri)null, ""); });

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadFileAsync((Uri)null, ""); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadFileAsync((Uri)null, "", null); });

            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.DownloadFileTaskAsync((string)null, ""); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadFileTaskAsync((Uri)null, ""); });

            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.DownloadFile("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.DownloadFile(new Uri("http://localhost"), null); });

            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.DownloadFileAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.DownloadFileAsync(new Uri("http://localhost"), null, null); });

            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.DownloadFileTaskAsync("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.DownloadFileTaskAsync(new Uri("http://localhost"), null); });
        }

        [Fact]
        public static void DownloadString_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadString((string)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadString((Uri)null); });

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadStringAsync((Uri)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadStringAsync((Uri)null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.DownloadStringTaskAsync((string)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.DownloadStringTaskAsync((Uri)null); });
        }

        [Fact]
        public static void UploadData_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadData((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadData((string)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadData((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadData((Uri)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadDataAsync((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadDataAsync((Uri)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadDataAsync((Uri)null, null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadDataTaskAsync((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadDataTaskAsync((string)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadDataTaskAsync((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadDataTaskAsync((Uri)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadData("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadData("http://localhost", null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadData(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadData(new Uri("http://localhost"), null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadDataAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadDataAsync(new Uri("http://localhost"), null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadDataAsync(new Uri("http://localhost"), null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadDataTaskAsync("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadDataTaskAsync("http://localhost", null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadDataTaskAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadDataTaskAsync(new Uri("http://localhost"), null, null); });
        }

        [Fact]
        public static void UploadFile_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadFile((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadFile((string)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadFile((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadFile((Uri)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadFileAsync((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadFileAsync((Uri)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadFileAsync((Uri)null, null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadFileTaskAsync((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadFileTaskAsync((string)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadFileTaskAsync((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadFileTaskAsync((Uri)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFile("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFile("http://localhost", null, null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFile(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFile(new Uri("http://localhost"), null, null); });

            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFileAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFileAsync(new Uri("http://localhost"), null, null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFileAsync(new Uri("http://localhost"), null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFileTaskAsync("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFileTaskAsync("http://localhost", null, null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFileTaskAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => { wc.UploadFileTaskAsync(new Uri("http://localhost"), null, null); });
        }

        [Fact]
        public static void UploadString_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();
            
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadString((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadString((string)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadString((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadString((Uri)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadStringAsync((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadStringAsync((Uri)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadStringAsync((Uri)null, null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadStringTaskAsync((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadStringTaskAsync((string)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadStringTaskAsync((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadStringTaskAsync((Uri)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadString("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadString("http://localhost", null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadString(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadString(new Uri("http://localhost"), null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadStringAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadStringAsync(new Uri("http://localhost"), null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadStringAsync(new Uri("http://localhost"), null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadStringTaskAsync("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadStringTaskAsync("http://localhost", null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadStringTaskAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadStringTaskAsync(new Uri("http://localhost"), null, null); });
        }

        [Fact]
        public static void UploadValues_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();
            
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValues((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValues((string)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValues((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValues((Uri)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValuesAsync((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValuesAsync((Uri)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValuesAsync((Uri)null, null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadValuesTaskAsync((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", "path", () => { wc.UploadValuesTaskAsync((string)null, null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValuesTaskAsync((Uri)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.UploadValuesTaskAsync((Uri)null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValues("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValues("http://localhost", null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValues(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValues(new Uri("http://localhost"), null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValuesAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValuesAsync(new Uri("http://localhost"), null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValuesAsync(new Uri("http://localhost"), null, null, null); });

            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValuesTaskAsync("http://localhost", null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValuesTaskAsync("http://localhost", null, null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValuesTaskAsync(new Uri("http://localhost"), null); });
            AssertExtensions.Throws<ArgumentNullException>("data", () => { wc.UploadValuesTaskAsync(new Uri("http://localhost"), null, null); });
        }

        [Fact]
        public static void OpenWrite_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.OpenWrite((string)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.OpenWrite((string)null, null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.OpenWrite((Uri)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.OpenWrite((Uri)null, null); });
        }

        [Fact]
        public static void OpenRead_InvalidArguments_ThrowExceptions()
        {
            var wc = new WebClient();

            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.OpenRead((string)null); });
            AssertExtensions.Throws<ArgumentNullException>("address", () => { wc.OpenRead((Uri)null); });
        }

        [Fact]
        public static void BaseAddress_Roundtrips()
        {
            var wc = new WebClient();
            wc.BaseAddress = "http://localhost/";
            Assert.Equal("http://localhost/", wc.BaseAddress);
            wc.BaseAddress = null;
            Assert.Equal(string.Empty, wc.BaseAddress);
        }

        [Fact]
        public static void CachePolicy_Roundtrips()
        {
            var wc = new WebClient();
            var c = new RequestCachePolicy(RequestCacheLevel.BypassCache);
            wc.CachePolicy = c;
            Assert.Same(c, wc.CachePolicy);
        }

        [Fact]
        public static void Credentials_Roundtrips()
        {
            var wc = new WebClient();
            var c = new DummyCredentials();
            wc.Credentials = c;
            Assert.Same(c, wc.Credentials);
            wc.Credentials = null;
            Assert.Null(wc.Credentials);
        }

        private sealed class DummyCredentials : ICredentials
        {
            public NetworkCredential GetCredential(Uri uri, string authType) => null;
        }

        [Fact]
        public static void Proxy_Roundtrips()
        {
            var wc = new WebClient();
            Assert.Same(WebRequest.DefaultWebProxy, wc.Proxy);

            var p = new DummyProxy();
            wc.Proxy = p;
            Assert.Same(p, wc.Proxy);

            wc.Proxy = null;
            Assert.Null(wc.Proxy);
        }

        private sealed class DummyProxy : IWebProxy
        {
            public ICredentials Credentials { get; set; }

            public Uri GetProxy(Uri destination) => null;
            public bool IsBypassed(Uri host) => false;
        }

        [Fact]
        public static void Encoding_Roundtrips()
        {
            var wc = new WebClient();
            Encoding e = Encoding.UTF8;
            wc.Encoding = e;
            Assert.Same(e, wc.Encoding);
        }

        [Fact]
        public static void Headers_Roundtrips()
        {
            var wc = new WebClient();
            Assert.NotNull(wc.Headers);
            Assert.Empty(wc.Headers);

            wc.Headers = null;
            Assert.NotNull(wc.Headers);
            Assert.Empty(wc.Headers);

            var whc = new WebHeaderCollection();
            wc.Headers = whc;
            Assert.Same(whc, wc.Headers);
        }

        [Fact]
        public static void QueryString_Roundtrips()
        {
            var wc = new WebClient();
            Assert.NotNull(wc.QueryString);
            Assert.Empty(wc.QueryString);

            wc.QueryString = null;
            Assert.NotNull(wc.QueryString);
            Assert.Empty(wc.QueryString);

            var nvc = new NameValueCollection();
            wc.QueryString = nvc;
            Assert.Same(nvc, wc.QueryString);
        }

        [Fact]
        public static void UseDefaultCredentials_Roundtrips()
        {
            var wc = new WebClient();
            for (int i = 0; i < 2; i++)
            {
                wc.UseDefaultCredentials = true;
                Assert.True(wc.UseDefaultCredentials);
                wc.UseDefaultCredentials = false;
                Assert.False(wc.UseDefaultCredentials);
            }
        }

        [Fact]
        public static async Task ResponseHeaders_ContainsHeadersAfterOperation()
        {
            var wc = new DerivedWebClient(); // verify we can use a derived type as well
            Assert.Null(wc.ResponseHeaders);

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                Task<string> download = wc.DownloadStringTaskAsync(url.ToString());
                Assert.Null(wc.ResponseHeaders);
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        "HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        $"Content-Length: 0\r\n" +
                        "ArbitraryHeader: ArbitraryValue\r\n" +
                        "\r\n");
                await download;
            });

            Assert.NotNull(wc.ResponseHeaders);
            Assert.Equal("ArbitraryValue", wc.ResponseHeaders["ArbitraryHeader"]);
        }

        [OuterLoop("Networking test talking to remote server: issue #11345")]
        [Theory]
        [InlineData("Connection", "close")]
        [InlineData("Expect", "100-continue")]
        public static async Task RequestHeaders_AddDisallowedHeaderAndSendRequest_ThrowsWebException(string headerName, string headerValue)
        {
            var wc = new WebClient();
            wc.Headers[headerName] = headerValue;
            await Assert.ThrowsAsync<WebException>(() => wc.DownloadStringTaskAsync(System.Net.Test.Common.Configuration.Http.RemoteEchoServer));
        }

        [OuterLoop("Networking test talking to remote server: issue #11345")]
        [Theory]
        [InlineData("http://localhost", true)]
        [InlineData("localhost", false)]
        public static async Task RequestHeaders_AddHostHeaderAndSendRequest_ExpectedResult(string hostHeaderValue, bool throwsWebException)
        {
            var wc = new WebClient();
            wc.Headers["Host"] = hostHeaderValue;
            if (throwsWebException)
            {
                await Assert.ThrowsAsync<WebException>(() => wc.DownloadStringTaskAsync(System.Net.Test.Common.Configuration.Http.RemoteEchoServer));
            }
            else
            {
                await wc.DownloadStringTaskAsync(System.Net.Test.Common.Configuration.Http.RemoteEchoServer);
            }
        }

        [Fact]
        public static async Task RequestHeaders_SpecialHeaders_RequestSucceeds()
        {
            var wc = new WebClient();

            wc.Headers["Accept"] = "text/html";
            wc.Headers["ContentType"] = "text/html; charset=utf-8";
            wc.Headers["Referer"] = "http://localhost";
            wc.Headers["User-Agent"] = ".NET";

            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                Task<string> download = wc.DownloadStringTaskAsync(url.ToString());
                Assert.Null(wc.ResponseHeaders);
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        "HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        $"Content-Length: 0\r\n" +
                        "\r\n");
                await download;
            });
        }

        [Fact]
        public static async Task ConcurrentOperations_Throw()
        {
            await LoopbackServer.CreateServerAsync((server, url) =>
            {
                var wc = new WebClient();
                Task ignored = wc.DownloadDataTaskAsync(url); // won't complete
                Assert.Throws<NotSupportedException>(() => { wc.DownloadData(url); });
                Assert.Throws<NotSupportedException>(() => { wc.DownloadDataAsync(url); });
                Assert.Throws<NotSupportedException>(() => { wc.DownloadDataTaskAsync(url); });
                Assert.Throws<NotSupportedException>(() => { wc.DownloadString(url); });
                Assert.Throws<NotSupportedException>(() => { wc.DownloadStringAsync(url); });
                Assert.Throws<NotSupportedException>(() => { wc.DownloadStringTaskAsync(url); });
                Assert.Throws<NotSupportedException>(() => { wc.DownloadFile(url, "path"); });
                Assert.Throws<NotSupportedException>(() => { wc.DownloadFileAsync(url, "path"); });
                Assert.Throws<NotSupportedException>(() => { wc.DownloadFileTaskAsync(url, "path"); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadData(url, new byte[42]); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadDataAsync(url, new byte[42]); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadDataTaskAsync(url, new byte[42]); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadString(url, "42"); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadStringAsync(url, "42"); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadStringTaskAsync(url, "42"); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadFile(url, "path"); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadFileAsync(url, "path"); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadFileTaskAsync(url, "path"); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadValues(url, new NameValueCollection()); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadValuesAsync(url, new NameValueCollection()); });
                Assert.Throws<NotSupportedException>(() => { wc.UploadValuesTaskAsync(url, new NameValueCollection()); });
                return Task.CompletedTask;
            });
        }

        private sealed class DerivedWebClient : WebClient { }
    }

    public abstract class WebClientTestBase
    {
        public static readonly object[][] EchoServers = System.Net.Test.Common.Configuration.Http.EchoServers;
        const string ExpectedText =
            "To be, or not to be, that is the question:" +
            "Whether 'tis Nobler in the mind to suffer" +
            "The Slings and Arrows of outrageous Fortune," +
            "Or to take Arms against a Sea of troubles," +
            "And by opposing end them:";
        
        const string ExpectedTextAfterUrlEncode = 
            "To+be%2c+or+not+to+be%2c+that+is+the+question%3a" + 
            "Whether+'tis+Nobler+in+the+mind+to+suffer" +
            "The+Slings+and+Arrows+of+outrageous+Fortune%2c" +
            "Or+to+take+Arms+against+a+Sea+of+troubles%2c" +
            "And+by+opposing+end+them%3a";

        protected abstract bool IsAsync { get; }

        protected abstract Task<byte[]> DownloadDataAsync(WebClient wc, string address);
        protected abstract Task DownloadFileAsync(WebClient wc, string address, string fileName);
        protected abstract Task<string> DownloadStringAsync(WebClient wc, string address);

        protected abstract Task<byte[]> UploadDataAsync(WebClient wc, string address, byte[] data);
        protected abstract Task<byte[]> UploadFileAsync(WebClient wc, string address, string fileName);
        protected abstract Task<string> UploadStringAsync(WebClient wc, string address, string data);
        protected abstract Task<byte[]> UploadValuesAsync(WebClient wc, string address, NameValueCollection data);

        protected abstract Task<Stream> OpenReadAsync(WebClient wc, string address);
        protected abstract Task<Stream> OpenWriteAsync(WebClient wc, string address);

        [Theory]
        [InlineData(null)]
        [InlineData("text/html; charset=utf-8")]
        [InlineData("text/html; charset=us-ascii")]
        public async Task DownloadString_Success(string contentType)
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                var wc = new WebClient();
                if (contentType != null)
                {
                    wc.Headers[HttpRequestHeader.ContentType] = contentType;
                }

                Task<string> download = DownloadStringAsync(wc, url.ToString());
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        "HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        $"Content-Length: {ExpectedText.Length}\r\n" +
                        "\r\n" +
                        $"{ExpectedText}");
                Assert.Equal(ExpectedText, await download);
            });
        }

        [Fact]
        public async Task DownloadData_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                var wc = new WebClient();

                var downloadProgressInvoked = new TaskCompletionSource<bool>();
                wc.DownloadProgressChanged += (s, e) => downloadProgressInvoked.TrySetResult(true);

                Task<byte[]> download = DownloadDataAsync(wc, url.ToString());
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        "HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        $"Content-Length: {ExpectedText.Length}\r\n" +
                        "\r\n" +
                        $"{ExpectedText}");
                Assert.Equal(ExpectedText, Encoding.ASCII.GetString(await download));
                Assert.True(!IsAsync || await downloadProgressInvoked.Task, "Expected download progress callback to be invoked");
            });
        }

        [Fact]
        public async Task DownloadData_LargeData_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                string largeText = GetRandomText(1024 * 1024);

                var wc = new WebClient();
                Task<byte[]> download = DownloadDataAsync(wc, url.ToString());
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        "HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        $"Content-Length: {largeText.Length}\r\n" +
                        "\r\n" +
                        $"{largeText}");
                Assert.Equal(largeText, Encoding.ASCII.GetString(await download));
            });
        }

        [Fact]
        public async Task DownloadFile_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                string tempPath = Path.GetTempFileName();
                try
                {
                    var wc = new WebClient();
                    Task download = DownloadFileAsync(wc, url.ToString(), tempPath);
                    await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                            "HTTP/1.1 200 OK\r\n" +
                            $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                            $"Content-Length: {ExpectedText.Length}\r\n" +
                            "\r\n" +
                            $"{ExpectedText}");

                    await download;
                    Assert.Equal(ExpectedText, File.ReadAllText(tempPath));
                }
                finally
                {
                    File.Delete(tempPath);
                }
            });
        }

        [Fact]
        public async Task OpenRead_Success()
        {
            await LoopbackServer.CreateServerAsync(async (server, url) =>
            {
                var wc = new WebClient();
                Task<Stream> download = OpenReadAsync(wc, url.ToString());
                await LoopbackServer.ReadRequestAndSendResponseAsync(server,
                        "HTTP/1.1 200 OK\r\n" +
                        $"Date: {DateTimeOffset.UtcNow:R}\r\n" +
                        $"Content-Length: {ExpectedText.Length}\r\n" +
                        "\r\n" +
                        $"{ExpectedText}");

                using (var reader = new StreamReader(await download))
                {
                    Assert.Equal(ExpectedText, await reader.ReadToEndAsync());
                }
            });
        }

        [OuterLoop("Networking test talking to remote server: issue #11345")]
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task OpenWrite_Success(Uri echoServer)
        {
            var wc = new WebClient();
            using (Stream s = await OpenWriteAsync(wc, echoServer.ToString()))
            {
                byte[] data = Encoding.UTF8.GetBytes(ExpectedText);
                await s.WriteAsync(data, 0, data.Length);
            }
        }

        [OuterLoop("Networking test talking to remote server: issue #11345")]
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task UploadData_Success(Uri echoServer)
        {
            var wc = new WebClient();

            var uploadProgressInvoked = new TaskCompletionSource<bool>();
            wc.UploadProgressChanged += (s, e) => uploadProgressInvoked.TrySetResult(true); // to enable chunking of the upload

            byte[] result = await UploadDataAsync(wc, echoServer.ToString(), Encoding.UTF8.GetBytes(ExpectedText));
            Assert.Contains(ExpectedText, Encoding.UTF8.GetString(result));
            Assert.True(!IsAsync || await uploadProgressInvoked.Task, "Expected upload progress callback to be invoked");
        }

        [OuterLoop("Networking test talking to remote server: issue #11345")]
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task UploadData_LargeData_Success(Uri echoServer)
        {
            var wc = new WebClient();
            string largeText = GetRandomText(512 * 1024);
            byte[] result = await UploadDataAsync(wc, echoServer.ToString(), Encoding.UTF8.GetBytes(largeText));
            Assert.Contains(largeText, Encoding.UTF8.GetString(result));
        }

        private static string GetRandomText(int length)
        {
            var rand = new Random();
            return new string(Enumerable.Range(0, 512 * 1024).Select(_ => (char)('a' + rand.Next(0, 26))).ToArray());
        }

        [OuterLoop("Networking test talking to remote server: issue #11345")]
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task UploadFile_Success(Uri echoServer)
        {
            string tempPath = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(tempPath, Encoding.UTF8.GetBytes(ExpectedText));
                var wc = new WebClient();
                byte[] result = await UploadFileAsync(wc, echoServer.ToString(), tempPath);
                Assert.Contains(ExpectedText, Encoding.UTF8.GetString(result));
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [OuterLoop("Networking test talking to remote server: issue #11345")]
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task UploadString_Success(Uri echoServer)
        {
            var wc = new WebClient();
            string result = await UploadStringAsync(wc, echoServer.ToString(), ExpectedText);
            Assert.Contains(ExpectedText, result);
        }

        [OuterLoop("Networking test talking to remote server: issue #11345")]
        [Theory]
        [MemberData(nameof(EchoServers))]
        public async Task UploadValues_Success(Uri echoServer)
        {
            var wc = new WebClient();
            byte[] result = await UploadValuesAsync(wc, echoServer.ToString(), new NameValueCollection() { { "Data", ExpectedText } });
            Assert.Contains(ExpectedTextAfterUrlEncode, Encoding.UTF8.GetString(result));
        }
    }

    public class SyncWebClientTest : WebClientTestBase
    {
        protected override bool IsAsync => false;

        protected override Task<byte[]> DownloadDataAsync(WebClient wc, string address) => Task.Run(() => wc.DownloadData(address));
        protected override Task DownloadFileAsync(WebClient wc, string address, string fileName) => Task.Run(() => wc.DownloadFile(address, fileName));
        protected override Task<string> DownloadStringAsync(WebClient wc, string address) => Task.Run(() => wc.DownloadString(address));

        protected override Task<byte[]> UploadDataAsync(WebClient wc, string address, byte[] data) => Task.Run(() => wc.UploadData(address, data));
        protected override Task<byte[]> UploadFileAsync(WebClient wc, string address, string fileName) => Task.Run(() => wc.UploadFile(address, fileName));
        protected override Task<string> UploadStringAsync(WebClient wc, string address, string data) => Task.Run(() => wc.UploadString(address, data));
        protected override Task<byte[]> UploadValuesAsync(WebClient wc, string address, NameValueCollection data) => Task.Run(() => wc.UploadValues(address, data));

        protected override Task<Stream> OpenReadAsync(WebClient wc, string address) => Task.Run(() => wc.OpenRead(address));
        protected override Task<Stream> OpenWriteAsync(WebClient wc, string address) => Task.Run(() => wc.OpenWrite(address));
    }

    // NOTE: Today the XxTaskAsync APIs are implemented as wrappers for the XxAsync APIs.
    // If that changes, we should add an EapWebClientTest here that targets those directly.
    // In the meantime, though, there's no benefit to the extra testing it would provide.
    public class TaskWebClientTest : WebClientTestBase
    {
        protected override bool IsAsync => true;

        protected override Task<byte[]> DownloadDataAsync(WebClient wc, string address) => wc.DownloadDataTaskAsync(address);
        protected override Task DownloadFileAsync(WebClient wc, string address, string fileName) => wc.DownloadFileTaskAsync(address, fileName);
        protected override Task<string> DownloadStringAsync(WebClient wc, string address) => wc.DownloadStringTaskAsync(address);

        protected override Task<byte[]> UploadDataAsync(WebClient wc, string address, byte[] data) => wc.UploadDataTaskAsync(address, data);
        protected override Task<byte[]> UploadFileAsync(WebClient wc, string address, string fileName) => wc.UploadFileTaskAsync(address, fileName);
        protected override Task<string> UploadStringAsync(WebClient wc, string address, string data) => wc.UploadStringTaskAsync(address, data);
        protected override Task<byte[]> UploadValuesAsync(WebClient wc, string address, NameValueCollection data) => wc.UploadValuesTaskAsync(address, data);

        protected override Task<Stream> OpenReadAsync(WebClient wc, string address) => wc.OpenReadTaskAsync(address);
        protected override Task<Stream> OpenWriteAsync(WebClient wc, string address) => wc.OpenWriteTaskAsync(address);
    }
}
