// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Test.Uris.UriParserTest
{
    public class WebSocketsUriParserTest
    {
        [Fact]
        public void WebSocketsUri_CreateWithAllParts_Success()
        {
            string input = "ws://user:pass@host:90/path1/path2/file.ext?query#fragment";
            Uri uri = new Uri(input);

            Assert.Equal(input, uri.ToString());
            Assert.Equal(input, uri.AbsoluteUri);
            Assert.Equal("ws", uri.Scheme);
            Assert.Equal("user:pass", uri.UserInfo);
            Assert.Equal("host", uri.Host);
            Assert.Equal(UriHostNameType.Dns, uri.HostNameType);
            Assert.Equal(90, uri.Port);
            Assert.Equal("/path1/path2/file.ext", uri.AbsolutePath);
            Assert.Equal("?query", uri.Query);
            Assert.Equal("#fragment", uri.Fragment);
        }

        [Fact]
        public void WebSocketsSecureUri_CreateWithAllParts_Success()
        {
            string input = "wss://user:pass@host:90/path1/path2/file.ext?query#fragment";
            Uri uri = new Uri(input);

            Assert.Equal(input, uri.ToString());
            Assert.Equal(input, uri.AbsoluteUri);
            Assert.Equal("wss", uri.Scheme);
            Assert.Equal("user:pass", uri.UserInfo);
            Assert.Equal("host", uri.Host);
            Assert.Equal(UriHostNameType.Dns, uri.HostNameType);
            Assert.Equal(90, uri.Port);
            Assert.Equal("/path1/path2/file.ext", uri.AbsolutePath);
            Assert.Equal("?query", uri.Query);
            Assert.Equal("#fragment", uri.Fragment);
        }

        [Fact]
        public void WebSocketsUri_TryCreateWithAllParts_Success()
        {
            string input = "ws://user:pass@host:90/path1/path2/file.ext?query#fragment";
            Uri uri;
            bool success = Uri.TryCreate(input, UriKind.Absolute, out uri);
            Assert.True(success);

            Assert.Equal(input, uri.ToString());
            Assert.Equal(input, uri.AbsoluteUri);
            Assert.Equal("ws", uri.Scheme);
            Assert.Equal("user:pass", uri.UserInfo);
            Assert.Equal("host", uri.Host);
            Assert.Equal(UriHostNameType.Dns, uri.HostNameType);
            Assert.Equal(90, uri.Port);
            Assert.Equal("/path1/path2/file.ext", uri.AbsolutePath);
            Assert.Equal("?query", uri.Query);
            Assert.Equal("#fragment", uri.Fragment);
        }

        [Fact]
        public void WebSocketsSecureUri_TryCreateWithAllParts_Success()
        {
            string input = "wss://user:pass@host:90/path1/path2/file.ext?query#fragment";
            Uri uri;
            bool success = Uri.TryCreate(input, UriKind.Absolute, out uri);
            Assert.True(success);

            Assert.Equal(input, uri.ToString());
            Assert.Equal(input, uri.AbsoluteUri);
            Assert.Equal("wss", uri.Scheme);
            Assert.Equal("user:pass", uri.UserInfo);
            Assert.Equal("host", uri.Host);
            Assert.Equal(UriHostNameType.Dns, uri.HostNameType);
            Assert.Equal(90, uri.Port);
            Assert.Equal("/path1/path2/file.ext", uri.AbsolutePath);
            Assert.Equal("?query", uri.Query);
            Assert.Equal("#fragment", uri.Fragment);
        }

        [Fact]
        public void WebSocketsUri_DefaultPorts_Success()
        {
            string input = "ws://host/";
            Uri uri = new Uri(input);

            Assert.Equal(input, uri.ToString());
            Assert.Equal(input, uri.AbsoluteUri);
            Assert.Equal("ws", uri.Scheme);
            Assert.Equal(80, uri.Port);
        }

        [Fact]
        public void WebSocketsSecureUri_DefaultPorts_Success()
        {
            string input = "wss://host/";
            Uri uri = new Uri(input);

            Assert.Equal(input, uri.ToString());
            Assert.Equal(input, uri.AbsoluteUri);
            Assert.Equal("wss", uri.Scheme);
            Assert.Equal(443, uri.Port);
        }
    }
}
