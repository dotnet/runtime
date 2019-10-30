// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Net.Test.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Data;
using System.Runtime.InteropServices.ComTypes;

namespace System.Net.Http.Functional.Tests
{
    public class HPackTest : HttpClientHandlerTestBase
    {
        protected override bool UseSocketsHttpHandler => true;
        protected override bool UseHttp2 => true;

        public HPackTest(ITestOutputHelper output) : base(output)
        {
        }

        private const string LiteralHeaderName = "x-literal-header";
        private const string LiteralHeaderValue = "testing 456";

        [Theory]
        [MemberData(nameof(HeaderEncodingTestData))]
        public async Task HPack_HeaderEncoding(string headerName, string expectedValue, byte[] expectedEncoding)
        {
            await Http2LoopbackServer.CreateClientAndServerAsync(
                async uri =>
                {
                    using HttpClient client = CreateHttpClient();

                    using HttpRequestMessage request = new HttpRequestMessage();
                    request.Method = HttpMethod.Post;
                    request.RequestUri = uri;
                    request.Version = HttpVersion.Version20;
                    request.Content = new StringContent("testing 123");
                    request.Headers.Add(LiteralHeaderName, LiteralHeaderValue);

                    (await client.SendAsync(request)).Dispose();
                },
                async server =>
                {
                    Http2LoopbackConnection connection = await server.EstablishConnectionAsync();
                    (int streamId, HttpRequestData requestData) = await connection.ReadAndParseRequestHeaderAsync();

                    HttpHeaderData header = requestData.Headers.Single(x => x.Name == headerName);
                    Assert.Equal(expectedValue, header.Value);
                    Assert.True(expectedEncoding.AsSpan().SequenceEqual(header.Raw));

                    await connection.SendDefaultResponseAsync(streamId);
                });
        }

        public static IEnumerable<object[]> HeaderEncodingTestData()
        {
            // Indexed name, indexed value.
            yield return new object[] { ":method", "POST", new byte[] { 0x83 } };
            yield return new object[] { ":path", "/", new byte[] { 0x84 } };

            // Indexed name, literal value.
            yield return new object[] { "content-type", "text/plain; charset=utf-8", new byte[] { 0x0F, 0x10, 0x19, 0x74, 0x65, 0x78, 0x74, 0x2F, 0x70, 0x6C, 0x61, 0x69, 0x6E, 0x3B, 0x20, 0x63, 0x68, 0x61, 0x72, 0x73, 0x65, 0x74, 0x3D, 0x75, 0x74, 0x66, 0x2D, 0x38 } };

            // Literal name, literal value.
            yield return new object[] { LiteralHeaderName, LiteralHeaderValue, new byte[] { 0x00, 0x10, 0x78, 0x2D, 0x6C, 0x69, 0x74, 0x65, 0x72, 0x61, 0x6C, 0x2D, 0x68, 0x65, 0x61, 0x64, 0x65, 0x72, 0x0B, 0x74, 0x65, 0x73, 0x74, 0x69, 0x6E, 0x67, 0x20, 0x34, 0x35, 0x36 } };
        }
    }
}
