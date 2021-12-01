// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices.JavaScript.Tests;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Runtime.InteropServices.JavaScript.Http.Tests
{
    public class HttpRequestMessageTest
    {
        private readonly Version _expectedRequestMessageVersion = HttpVersion.Version11;
        private HttpRequestOptionsKey<bool> EnableStreamingResponse = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse");
#nullable enable
        private HttpRequestOptionsKey<IDictionary<string, object?>> FetchOptions = new HttpRequestOptionsKey<IDictionary<string, object?>>("WebAssemblyFetchOptions");
#nullable disable

        [Fact]
        public void Ctor_Default_CorrectDefaults()
        {
            var rm = new HttpRequestMessage();

            Assert.Equal(HttpMethod.Get, rm.Method);
            Assert.Null(rm.Content);
            Assert.Null(rm.RequestUri);
        }

        [Fact]
        public void Ctor_RelativeStringUri_CorrectValues()
        {
            var rm = new HttpRequestMessage(HttpMethod.Post, "/relative");

            Assert.Equal(HttpMethod.Post, rm.Method);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Null(rm.Content);
            Assert.Equal(new Uri("/relative", UriKind.Relative), rm.RequestUri);
        }

        [Theory]
        [InlineData("http://host/absolute/")]
        [InlineData("blob:http://host/absolute/")]
        [InlineData("foo://host/absolute")]
        public void Ctor_AbsoluteStringUri_CorrectValues(string uri)
        {
            var rm = new HttpRequestMessage(HttpMethod.Post, uri);

            Assert.Equal(HttpMethod.Post, rm.Method);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Null(rm.Content);
            Assert.Equal(new Uri(uri), rm.RequestUri);
        }

        [Fact]
        public void Ctor_NullStringUri_Accepted()
        {
            var rm = new HttpRequestMessage(HttpMethod.Put, (string)null);

            Assert.Null(rm.RequestUri);
            Assert.Equal(HttpMethod.Put, rm.Method);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Null(rm.Content);
        }

        [Fact]
        public void Ctor_RelativeUri_CorrectValues()
        {
            var uri = new Uri("/relative", UriKind.Relative);
            var rm = new HttpRequestMessage(HttpMethod.Post, uri);

            Assert.Equal(HttpMethod.Post, rm.Method);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Null(rm.Content);
            Assert.Equal(uri, rm.RequestUri);
        }

        [Theory]
        [InlineData("http://host/absolute/")]
        [InlineData("blob:http://host/absolute/")]
        [InlineData("foo://host/absolute")]
        public void Ctor_AbsoluteUri_CorrectValues(string uriData)
        {
            var uri = new Uri(uriData);
            var rm = new HttpRequestMessage(HttpMethod.Post, uri);

            Assert.Equal(HttpMethod.Post, rm.Method);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Null(rm.Content);
            Assert.Equal(uri, rm.RequestUri);
        }

        [Fact]
        public void Ctor_NullUri_Accepted()
        {
            var rm = new HttpRequestMessage(HttpMethod.Put, (Uri)null);

            Assert.Null(rm.RequestUri);
            Assert.Equal(HttpMethod.Put, rm.Method);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Null(rm.Content);
        }

        [Theory]
        [InlineData("http://example.com")]
        [InlineData("blob:http://example.com")]
        public void Ctor_NullMethod_ThrowsArgumentNullException(string uriData)
        {
            Assert.Throws<ArgumentNullException>(() => new HttpRequestMessage(null, uriData));
        }

        [Theory]
        [InlineData("http://example.com")]
        [InlineData("blob:http://example.com")]
        public void Dispose_DisposeObject_ContentGetsDisposedAndSettersWillThrowButGettersStillWork(string uriData)
        {
            var rm = new HttpRequestMessage(HttpMethod.Get, uriData);
            var content = new MockContent();
            rm.Content = content;
            Assert.False(content.IsDisposed);

            rm.Dispose();
            rm.Dispose(); // Multiple calls don't throw.

            Assert.True(content.IsDisposed);
            Assert.Throws<ObjectDisposedException>(() => { rm.Method = HttpMethod.Put; });
            Assert.Throws<ObjectDisposedException>(() => { rm.RequestUri = null; });
            Assert.Throws<ObjectDisposedException>(() => { rm.Version = new Version(1, 0); });
            Assert.Throws<ObjectDisposedException>(() => { rm.Content = null; });

            // Property getters should still work after disposing.
            Assert.Equal(HttpMethod.Get, rm.Method);
            Assert.Equal(new Uri(uriData), rm.RequestUri);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Equal(content, rm.Content);
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("blob:https://example.com")]
        public void Properties_SetOptionsAndGetTheirValue_MatchingValues(string uriData)
        {
            var rm = new HttpRequestMessage();

            var content = new MockContent();
            var uri = new Uri(uriData);
            var version = new Version(1, 0);
            var method = new HttpMethod("custom");

            rm.Content = content;
            rm.Method = method;
            rm.RequestUri = uri;
            rm.Version = version;

            Assert.Equal(content, rm.Content);
            Assert.Equal(uri, rm.RequestUri);
            Assert.Equal(method, rm.Method);
            Assert.Equal(version, rm.Version);

            Assert.NotNull(rm.Headers);
            Assert.NotNull(rm.Options);
        }

#nullable enable
        [Theory]
        [InlineData("https://example.com")]
        [InlineData("blob:https://example.com")]
        public void Properties_SetOptionsAndGetTheirValue_Set_FetchOptions(string uriData)
        {
            var rm = new HttpRequestMessage();

            var content = new MockContent();
            var uri = new Uri(uriData);
            var version = new Version(1, 0);
            var method = new HttpMethod("custom");

            rm.Content = content;
            rm.Method = method;
            rm.RequestUri = uri;
            rm.Version = version;

            var fetchme = new Dictionary<string, object?>();
            fetchme.Add("hic", null);
            fetchme.Add("sunt", 4444);
            fetchme.Add("dracones", new List<string>());
            rm.Options.Set(FetchOptions, fetchme);

            Assert.Equal(content, rm.Content);
            Assert.Equal(uri, rm.RequestUri);
            Assert.Equal(method, rm.Method);
            Assert.Equal(version, rm.Version);

            Assert.NotNull(rm.Headers);
            Assert.NotNull(rm.Options);

            rm.Options.TryGetValue(FetchOptions, out IDictionary<string, object?>? fetchOptionsValue);
            Assert.NotNull(fetchOptionsValue);
            if (fetchOptionsValue != null)
            {
                foreach (var item in fetchOptionsValue)
                {
                    Assert.True(fetchme.ContainsKey(item.Key));
                }
            }
        }
#nullable disable

#nullable enable
        [Theory]
        [InlineData("https://example.com")]
        [InlineData("blob:https://example.com")]
        public void Properties_SetOptionsAndGetTheirValue_NotSet_FetchOptions(string uriData)
        {
            var rm = new HttpRequestMessage();

            var content = new MockContent();
            var uri = new Uri(uriData);
            var version = new Version(1, 0);
            var method = new HttpMethod("custom");

            rm.Content = content;
            rm.Method = method;
            rm.RequestUri = uri;
            rm.Version = version;

            Assert.Equal(content, rm.Content);
            Assert.Equal(uri, rm.RequestUri);
            Assert.Equal(method, rm.Method);
            Assert.Equal(version, rm.Version);

            Assert.NotNull(rm.Headers);
            Assert.NotNull(rm.Options);

            rm.Options.TryGetValue(FetchOptions, out IDictionary<string, object?>? fetchOptionsValue);
            Assert.Null(fetchOptionsValue);
        }
#nullable disable
        [Theory]
        [InlineData("https://example.com")]
        [InlineData("blob:https://example.com")]
        public void Properties_SetOptionsAndGetTheirValue_Set_EnableStreamingResponse(string uriData)
        {
            var rm = new HttpRequestMessage();

            var content = new MockContent();
            var uri = new Uri(uriData);
            var version = new Version(1, 0);
            var method = new HttpMethod("custom");

            rm.Content = content;
            rm.Method = method;
            rm.RequestUri = uri;
            rm.Version = version;

            rm.Options.Set(EnableStreamingResponse, true);

            Assert.Equal(content, rm.Content);
            Assert.Equal(uri, rm.RequestUri);
            Assert.Equal(method, rm.Method);
            Assert.Equal(version, rm.Version);

            Assert.NotNull(rm.Headers);
            Assert.NotNull(rm.Options);

            rm.Options.TryGetValue(EnableStreamingResponse, out bool streamingEnabledValue);
            Assert.True(streamingEnabledValue);
        }

        [Theory]
        [InlineData("https://example.com")]
        [InlineData("blob:https://example.com")]
        public void Properties_SetOptionsAndGetTheirValue_NotSet_EnableStreamingResponse(string uriData)
        {
            var rm = new HttpRequestMessage();

            var content = new MockContent();
            var uri = new Uri(uriData);
            var version = new Version(1, 0);
            var method = new HttpMethod("custom");

            rm.Content = content;
            rm.Method = method;
            rm.RequestUri = uri;
            rm.Version = version;

            Assert.Equal(content, rm.Content);
            Assert.Equal(uri, rm.RequestUri);
            Assert.Equal(method, rm.Method);
            Assert.Equal(version, rm.Version);

            Assert.NotNull(rm.Headers);
            Assert.NotNull(rm.Options);

            rm.Options.TryGetValue(EnableStreamingResponse, out bool streamingEnabledValue);
            Assert.False(streamingEnabledValue);
        }

        [Fact]
        public void Version_SetToNull_ThrowsArgumentNullException()
        {
            var rm = new HttpRequestMessage();
            Assert.Throws<ArgumentNullException>(() => { rm.Version = null; });
        }

        [Fact]
        public void Method_SetToNull_ThrowsArgumentNullException()
        {
            var rm = new HttpRequestMessage();
            Assert.Throws<ArgumentNullException>(() => { rm.Method = null; });
        }

        [Fact]
        public void ToString_DefaultInstance_DumpAllFields()
        {
            var rm = new HttpRequestMessage();
            string expected =
                    "Method: GET, RequestUri: '<null>', Version: " +
                    _expectedRequestMessageVersion.ToString(2) +
                    $", Content: <null>, Headers:{Environment.NewLine}{{{Environment.NewLine}}}";
            Assert.Equal(expected, rm.ToString());
        }

        [Theory]
        [InlineData("http://a.com/")]
        [InlineData("blob:http://a.com/")]
        public void ToString_NonDefaultInstanceWithNoCustomHeaders_DumpAllFields(string uriData)
        {
            var rm = new HttpRequestMessage();
            rm.Method = HttpMethod.Put;
            rm.RequestUri = new Uri(uriData);
            rm.Version = new Version(1, 0);
            rm.Content = new StringContent("content");

            // Note that there is no Content-Length header: The reason is that the value for Content-Length header
            // doesn't get set by StringContent..ctor, but only if someone actually accesses the ContentLength property.
            Assert.Equal(
                $"Method: PUT, RequestUri: '{uriData}', Version: 1.0, Content: " + typeof(StringContent).ToString() + ", Headers:" + Environment.NewLine +
                $"{{{Environment.NewLine}" +
                "  Content-Type: text/plain; charset=utf-8" + Environment.NewLine +
                "}", rm.ToString());
        }

        [Theory]
        [InlineData("http://a.com/")]
        [InlineData("blob:http://a.com/")]
        public void ToString_NonDefaultInstanceWithCustomHeaders_DumpAllFields(string uriData)
        {
            var rm = new HttpRequestMessage();
            rm.Method = HttpMethod.Put;
            rm.RequestUri = new Uri(uriData);
            rm.Version = new Version(1, 0);
            rm.Content = new StringContent("content");
            rm.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain", 0.2));
            rm.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml", 0.1));
            rm.Headers.Add("Custom-Request-Header", "value1");
            rm.Content.Headers.Add("Custom-Content-Header", "value2");

            Assert.Equal(
                $"Method: PUT, RequestUri: '{uriData}', Version: 1.0, Content: " + typeof(StringContent).ToString() + ", Headers:" + Environment.NewLine +
                "{" + Environment.NewLine +
                "  Accept: text/plain; q=0.2" + Environment.NewLine +
                "  Accept: text/xml; q=0.1" + Environment.NewLine +
                "  Custom-Request-Header: value1" + Environment.NewLine +
                "  Content-Type: text/plain; charset=utf-8" + Environment.NewLine +
                "  Custom-Content-Header: value2" + Environment.NewLine +
                "}", rm.ToString());
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsBrowserDomSupported))]
        public async Task BlobUri_Marshal_CorrectValues_Browser()
        {
            Runtime.InvokeJS(@"
                function typedArrayToURL(typedArray, mimeType) {
                    return URL.createObjectURL(new Blob([typedArray.buffer], {type: mimeType}))
                }
                const bytes = new Uint8Array(59);
                for(let i = 0; i < 59; i++) {
                    bytes[i] = 32 + i;
                }
                const url = typedArrayToURL(bytes, 'text/plain');
                // Calls method with string that will be marshaled as valid URI
                App.call_test_method  (""InvokeString"", [ url ]);
            ");

            var client = new HttpClient ();
            Assert.StartsWith ("blob:", HelperMarshal._stringResource);

            HttpRequestMessage rm = new HttpRequestMessage(HttpMethod.Get, new Uri (HelperMarshal._stringResource));
            HttpResponseMessage resp = await client.SendAsync (rm);
            Assert.NotNull (resp.Content);
            string content = await resp.Content.ReadAsStringAsync();
            Assert.Equal (59, content.Length);
        }

        [Fact]
        public void BlobStringUri_Marshal_CorrectValues()
        {
            Runtime.InvokeJS(@"
                function typedArrayToURL(typedArray, mimeType) {
                    // URL.createObjectURL does not work outside of browser but since this was actual
                    // test code from https://developer.mozilla.org/en-US/docs/Web/API/Blob
                    // left it in to show what this should do if the test code were to actually run
                    //return URL.createObjectURL(new Blob([typedArray.buffer], {type: mimeType}))
                    return 'blob:https://mdn.mozillademos.org/ca45b575-6348-4d3e-908a-3dbf3d146ea7';
                }
                const bytes = new Uint8Array(59);
                for(let i = 0; i < 59; i++) {
                    bytes[i] = 32 + i;
                }
                const url = typedArrayToURL(bytes, 'text/plain');
                // Calls method with string that will be converted to a valid Uri
                // within the method
                App.call_test_method  (""SetBlobUrl"", [ url ]);
            ");

            var rm = new HttpRequestMessage(HttpMethod.Post, HelperMarshal._blobURL);

            Assert.Equal(HttpMethod.Post, rm.Method);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Null(rm.Content);
            Assert.Equal(new Uri("blob:https://mdn.mozillademos.org/ca45b575-6348-4d3e-908a-3dbf3d146ea7"), rm.RequestUri);
        }

        [Fact]
        public void BlobUri_Marshal_CorrectValues()
        {
            Runtime.InvokeJS(@"
                function typedArrayToURL(typedArray, mimeType) {
                    // URL.createObjectURL does not work outside of browser but since this was actual
                    // test code from https://developer.mozilla.org/en-US/docs/Web/API/Blob
                    // left it in to show what this should do if the test code were to actually run
                    //return URL.createObjectURL(new Blob([typedArray.buffer], {type: mimeType}))
                    return 'blob:https://mdn.mozillademos.org/ca45b575-6348-4d3e-908a-3dbf3d146ea7';
                }
                const bytes = new Uint8Array(59);
                for(let i = 0; i < 59; i++) {
                    bytes[i] = 32 + i;
                }
                const url = typedArrayToURL(bytes, 'text/plain');
                // Calls method with string that will be marshaled as valid URI
                App.call_test_method  (""SetBlobAsUri"", [ url ]);
            ");

            var rm = new HttpRequestMessage(HttpMethod.Post, HelperMarshal._blobURI);

            Assert.Equal(HttpMethod.Post, rm.Method);
            Assert.Equal(_expectedRequestMessageVersion, rm.Version);
            Assert.Null(rm.Content);
            Assert.Equal(new Uri("blob:https://mdn.mozillademos.org/ca45b575-6348-4d3e-908a-3dbf3d146ea7"), rm.RequestUri);
        }

        #region Helper methods

        private class MockContent : HttpContent
        {
            public bool IsDisposed { get; private set; }

            protected override bool TryComputeLength(out long length)
            {
                throw new NotImplementedException();
            }

#nullable enable
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            {
#nullable disable
                throw new NotImplementedException();
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        #endregion
    }
}
