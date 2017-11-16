// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Tests
{
    [ActiveIssue(18784, TargetFrameworkMonikers.Uap)]
    public class HttpListenerResponseHeadersTests : HttpListenerResponseTestBase
    {
        private static string s_longString = new string('a', ushort.MaxValue + 1);

        [Fact]
        public async Task AddHeader_ValidValue_ReplacesHeaderInCollection()
        {
            HttpListenerResponse response = await GetResponse();

            response.AddHeader("name", "value1");
            Assert.Equal("value1", response.Headers["name"]);

            response.AddHeader("name", "value2");
            Assert.Equal("value2", response.Headers["name"]);
        }

        [Fact]
        public async Task AddHeader_NullOrEmptyName_ThrowsArgumentNullException()
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentNullException>("name", () => response.AddHeader(null, ""));
            AssertExtensions.Throws<ArgumentNullException>("name", () => response.AddHeader("", ""));
        }

        [Fact]
        [ActiveIssue(22057, TargetFrameworkMonikers.Netcoreapp)]
        public async Task AddHeader_LongName_ThrowsArgumentOutOfRangeException()
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => response.AddHeader("name", s_longString));
        }

        [Fact]
        public async Task AddHeader_InvalidNameOrValue_ThrowsArgumentException()
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentException>("name", () => response.AddHeader("\r \t \n", ""));
            AssertExtensions.Throws<ArgumentException>("name", () => response.AddHeader("(", ""));
            AssertExtensions.Throws<ArgumentException>("value", () => response.AddHeader("name", "value1\rvalue2\r"));
        }

        [Fact]
        public async Task AppendHeader_ValidValue_AddsHeaderToCollection()
        {
            HttpListenerResponse response = await GetResponse();

            response.AppendHeader("name", "value1");
            Assert.Equal("value1", response.Headers["name"]);

            response.AppendHeader("name", "value2");
            Assert.Equal("value1,value2", response.Headers["name"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task AppendHeader_NullOrEmptyName_ThrowsArgumentNullException(string name)
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentNullException>("name", () => response.AppendHeader(null, ""));
        }

        [Fact]
        [ActiveIssue(22057, TargetFrameworkMonikers.Netcoreapp)]
        public async Task AppendHeader_LongName_ThrowsArgumentOutOfRangeException()
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => response.AppendHeader("name", s_longString));
        }

        [Fact]
        public async Task AppendHeader_InvalidNameOrValue_ThrowsArgumentException()
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentException>("name", () => response.AppendHeader("\r \t \n", ""));
            AssertExtensions.Throws<ArgumentException>("name", () => response.AppendHeader("(", ""));
            AssertExtensions.Throws<ArgumentException>("value", () => response.AppendHeader("name", "value1\rvalue2\r"));
        }

        [Fact]
        public async Task ContentEncoding_SetCustom_DoesNothing()
        {
            // Setting HttpListenerResponse.ContentEncoding does nothing - it is never used.
            HttpListenerResponse response = await GetResponse();
            Assert.Null(response.ContentEncoding);

            response.ContentEncoding = Encoding.Unicode;
            Assert.Equal(Encoding.Unicode, response.ContentEncoding);
            response.Close();

            string clientResponse = GetClientResponse(120);
            Assert.DoesNotContain("Content-Encoding", clientResponse);
        }

        [Fact]
        public async Task ContentEncoding_SetDisposed_DoesNothing()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            response.ContentEncoding = Encoding.Unicode;
            Assert.Equal(Encoding.Unicode, response.ContentEncoding);

            string clientResponse = GetClientResponse(120);
            Assert.DoesNotContain("Content-Encoding", clientResponse);
        }

        [Fact]
        public async Task ContentEncoding_SetAfterHeadersSent_DoesNothing()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);

                response.ContentEncoding = Encoding.Unicode;
                Assert.Equal(Encoding.Unicode, response.ContentEncoding);
            }

            string clientResponse = GetClientResponse(111);
            Assert.DoesNotContain("Content-Encoding", clientResponse);
        }

        [Theory]
        [InlineData("application/json", 152)]
        [InlineData("  applICATion/jSOn   ", 152)]
        [InlineData("garbage", 143)]
        public async Task ContentType_SetAndSend_Success(string contentType, int expectedNumberOfBytes)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentType = contentType;
                Assert.Equal(contentType.Trim(), response.ContentType);
                Assert.Equal(contentType.Trim(), response.Headers[HttpResponseHeader.ContentType]);
            }

            string clientResponse = GetClientResponse(expectedNumberOfBytes);
            Assert.Contains($"\r\nContent-Type: {contentType.Trim()}\r\n", clientResponse);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("\r \t \n", "")]
        public async Task ContentType_SetNullEmptyOrWhitespace_ResetsContentType(string contentType, string expectedContentType)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentType = "application/json";
                Assert.Equal("application/json", response.Headers[HttpResponseHeader.ContentType]);

                response.ContentType = contentType;
                Assert.Equal(expectedContentType, response.ContentType);
                Assert.Equal(expectedContentType, response.Headers[HttpResponseHeader.ContentType]);
            }

            string clientResponse = GetClientResponse(120);
            Assert.DoesNotContain("Content-Encoding", clientResponse);
        }

        [Fact]
        public async Task ContentType_SetDisposed_ThrowsObjectDisposedException()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            Assert.Throws<ObjectDisposedException>(() => response.ContentType = "application/json");
            Assert.Null(response.ContentType);
        }

        [Fact]
        public async Task ContentType_SetAfterHeadersSent_DoesNothing()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);

                response.ContentType = "application/json";
                Assert.Equal("application/json", response.ContentType);
            }

            string clientResponse = GetClientResponse(111);
            Assert.DoesNotContain("Content-Type", clientResponse);
        }

        [Fact]
        public async Task OutputStream_GetDisposed_ThrowsObjectDisposedException()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            Assert.Throws<ObjectDisposedException>(() => response.OutputStream);
        }

        [Theory]
        [InlineData("http://microsoft.com", 152)]
        [InlineData("  http://MICROSOFT.com   ", 152)]
        [InlineData("garbage", 139)]
        [InlineData("http://domain:-1", 148)]
        public async Task RedirectLocation_SetAndSend_Success(string redirectLocation, int expectedNumberOfBytes)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.RedirectLocation = redirectLocation;
                Assert.Equal(redirectLocation.Trim(), response.RedirectLocation);
                Assert.Equal(redirectLocation.Trim(), response.Headers[HttpResponseHeader.Location]);
            }

            string clientResponse = GetClientResponse(expectedNumberOfBytes);
            Assert.Contains($"\r\nLocation: {redirectLocation.Trim()}\r\n", clientResponse);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("\r \t \n", "")]
        public async Task RedirectLocation_SetNullOrEmpty_ResetsRedirectLocation(string redirectLocation, string expectedRedirectLocation)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.RedirectLocation = "http://microsoft.com";
                Assert.Equal("http://microsoft.com", response.Headers[HttpResponseHeader.Location]);

                response.RedirectLocation = redirectLocation;
                Assert.Equal(expectedRedirectLocation, response.RedirectLocation);
                Assert.Equal(expectedRedirectLocation, response.Headers[HttpResponseHeader.Location]);

                // Setting RedirectLocation doesn't set the Redirect (302) status code.
                Assert.Equal(200, response.StatusCode);
            }

            string clientResponse = GetClientResponse(120);
            Assert.DoesNotContain("Location", clientResponse);
        }

        [Fact]
        public async Task RedirectLocation_SetDisposed_ThrowsObjectDisposedException()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            Assert.Throws<ObjectDisposedException>(() => response.RedirectLocation = "http://microsoft.com");
            Assert.Null(response.RedirectLocation);
        }

        [Fact]
        public async Task RedirectLocation_SetAfterHeadersSent_DoesNothing()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);

                response.RedirectLocation = "http://microsoft.com";
                Assert.Equal("http://microsoft.com", response.RedirectLocation);
            }

            string clientResponse = GetClientResponse(111);
            Assert.DoesNotContain("Location", clientResponse);
        }

        [Theory]
        [InlineData(100, "HTTP/1.1 100 Continue", 112)]
        [InlineData(404, "HTTP/1.1 404 Not Found", 127)]
        [InlineData(401, "HTTP/1.1 401 Unauthorized", 130)]
        [InlineData(999, "HTTP/1.1 999 ", 118)]
        public async Task StatusCode_SetAndSend_Success(int statusCode, string startLine, int expectedNumberOfBytes)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                Assert.Equal(200, response.StatusCode);

                response.StatusCode = statusCode;
                Assert.Equal(statusCode, response.StatusCode);
            }

            string clientResponse = GetClientResponse(expectedNumberOfBytes);
            Assert.StartsWith($"{startLine}\r\n", clientResponse);
        }

        [Fact]
        public async Task StatusCode_SetDisposed_ThrowsObjectDisposedException()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            Assert.Throws<ObjectDisposedException>(() => response.StatusCode = 100);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public async Task StatusCode_SetAfterHeadersSent_DoesNothing()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);

                response.StatusCode = 404;
                Assert.Equal(404, response.StatusCode);
            }

            string clientResponse = GetClientResponse(111);
            Assert.StartsWith("HTTP/1.1 200 OK\r\n", clientResponse);
        }

        [Theory]
        [InlineData(99)]
        [InlineData(1000)]
        public async Task StatusCode_SetInvalid_ThrowsProtocolViolationException(int statusCode)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                Assert.Throws<ProtocolViolationException>(() => response.StatusCode = statusCode);
                Assert.Equal(200, response.StatusCode);
            }
        }

        [Theory]
        [InlineData(100, "Continue")]
        [InlineData(101, "Switching Protocols")]
        [InlineData(102, "Processing")]
        [InlineData(200, "OK")]
        [InlineData(201, "Created")]
        [InlineData(202, "Accepted")]
        [InlineData(203, "Non-Authoritative Information")]
        [InlineData(204, "No Content")]
        [InlineData(205, "Reset Content")]
        [InlineData(206, "Partial Content")]
        [InlineData(207, "Multi-Status")]
        [InlineData(300, "Multiple Choices")]
        [InlineData(301, "Moved Permanently")]
        [InlineData(302, "Found")]
        [InlineData(303, "See Other")]
        [InlineData(304, "Not Modified")]
        [InlineData(305, "Use Proxy")]
        [InlineData(307, "Temporary Redirect")]
        [InlineData(400, "Bad Request")]
        [InlineData(401, "Unauthorized")]
        [InlineData(402, "Payment Required")]
        [InlineData(403, "Forbidden")]
        [InlineData(404, "Not Found")]
        [InlineData(405, "Method Not Allowed")]
        [InlineData(406, "Not Acceptable")]
        [InlineData(407, "Proxy Authentication Required")]
        [InlineData(408, "Request Timeout")]
        [InlineData(409, "Conflict")]
        [InlineData(410, "Gone")]
        [InlineData(411, "Length Required")]
        [InlineData(412, "Precondition Failed")]
        [InlineData(413, "Request Entity Too Large")]
        [InlineData(414, "Request-Uri Too Long")]
        [InlineData(415, "Unsupported Media Type")]
        [InlineData(416, "Requested Range Not Satisfiable")]
        [InlineData(417, "Expectation Failed")]
        [InlineData(422, "Unprocessable Entity")]
        [InlineData(423, "Locked")]
        [InlineData(424, "Failed Dependency")]
        [InlineData(426, "Upgrade Required")]
        [InlineData(500, "Internal Server Error")]
        [InlineData(501, "Not Implemented")]
        [InlineData(502, "Bad Gateway")]
        [InlineData(503, "Service Unavailable")]
        [InlineData(504, "Gateway Timeout")]
        [InlineData(505, "Http Version Not Supported")]
        [InlineData(507, "Insufficient Storage")]
        [InlineData(999, "")]
        public async Task StatusDescription_GetWithCustomStatusCode_ReturnsExpected(int statusCode, string expectedDescription)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.StatusCode = statusCode;
                Assert.Equal(expectedDescription, response.StatusDescription);

                // Changing the status code should do nothing.
                response.StatusCode = 404;
                Assert.Equal(expectedDescription, response.StatusDescription);
            }

            string clientResponse = GetClientResponse(118 + expectedDescription.Length);
            Assert.StartsWith($"HTTP/1.1 404 {expectedDescription}\r\n", clientResponse);
        }

        [Theory]
        [InlineData("", "", 118)]
        [InlineData("A !#\t1\u1234", "A !#\t14", 125)] // 
        [InlineData("StatusDescription", "StatusDescription", 135)]
        [InlineData("  StatusDescription  ", "  StatusDescription  ", 139)]
        public async Task StatusDescription_SetCustom_Success(string statusDescription, string expectedStatusDescription, int expectedNumberOfBytes)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.StatusDescription = statusDescription;
                Assert.Equal(statusDescription, response.StatusDescription);
            }

            string clientResponse = GetClientResponse(expectedNumberOfBytes);
            Assert.StartsWith($"HTTP/1.1 200 {expectedStatusDescription}\r\n", clientResponse);
        }
        
        [Fact]
        public async Task StatusDescription_SetNull_ThrowsArgumentNullException()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => response.StatusDescription = null);
                Assert.Equal("OK", response.StatusDescription);
            }
        }

        [Theory]
        [InlineData("\0abc")]
        [InlineData("\u007F")]
        [InlineData("\r")]
        [InlineData("\n")]
        public async Task StatusDescription_SetInvalid_ThrowsArgumentException(string statusDescription)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                AssertExtensions.Throws<ArgumentException>("name", () => response.StatusDescription = statusDescription);
                Assert.Equal("OK", response.StatusDescription);
            }
        }

        [Fact]
        public async Task StatusDescription_SetDisposed_ThrowsObjectDisposedException()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            Assert.Throws<ObjectDisposedException>(() => response.StatusDescription = "Hello");
            Assert.Equal("OK", response.StatusDescription);
        }

        [Fact]
        public async Task StatusDescription_SetAfterHeadersSent_DoesNothing()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);

                response.StatusDescription = "Hello";
                Assert.Equal("Hello", response.StatusDescription);
            }

            string clientResponse = GetClientResponse(111);
            Assert.StartsWith("HTTP/1.1 200 OK\r\n", clientResponse);
        }

        [Theory]
        [InlineData(true, 120)]
        [InlineData(false, 106)]
        public async Task SendChunked_GetSet_ReturnsExpected(bool sendChunked, int expectedNumberOfBytes)
        {
            HttpListenerResponse response = await GetResponse();
            try
            {
                Assert.False(response.SendChunked);
                Assert.Null(response.Headers[HttpResponseHeader.TransferEncoding]);

                response.SendChunked = sendChunked;
                Assert.Equal(sendChunked, response.SendChunked);
                Assert.Equal(sendChunked ? -1 : 0, response.ContentLength64);
                Assert.Null(response.Headers[HttpResponseHeader.TransferEncoding]);
            }
            finally
            {
                response.Close();
            }

            // The Transfer-Encoding: chunked header should be added to the list of headers if SendChunked == true.
            Assert.Equal(sendChunked ? "chunked" : null, response.Headers[HttpResponseHeader.TransferEncoding]);

            string clientResponse = GetClientResponse(expectedNumberOfBytes);
            if (sendChunked)
            {
                Assert.Contains("\r\nTransfer-Encoding: chunked\r\n", clientResponse);
            }
            else
            {
                Assert.DoesNotContain("Transfer-Encoding", clientResponse);
            }
        }

        [Fact]
        public async Task SendChunked_SetDisposed_ThrowsObjectDisposedException()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            Assert.Throws<ObjectDisposedException>(() => response.SendChunked = false);
            Assert.True(response.SendChunked);

            string clientResponse = GetClientResponse(120);
            Assert.Contains("\r\nTransfer-Encoding: chunked\r\n", clientResponse);
        }

        [Fact]
        public async Task SendChunked_SetAfterHeadersSent_ThrowsInvalidOperationException()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);

                Assert.Throws<InvalidOperationException>(() => response.SendChunked = true);
                Assert.False(response.SendChunked);
            }

            string clientResponse = GetClientResponse(111);
            Assert.DoesNotContain("Transfer-Encoding", clientResponse);
        }
        
        [Fact]
        public async Task SendChunked_SetTrueAndRequestHttpVersionMinorIsZero_ThrowsInvalidOperationException()
        {
            using (HttpListenerResponse response = await GetResponse("1.0"))
            {
                Assert.Throws<ProtocolViolationException>(() => response.SendChunked = true);
                Assert.False(response.SendChunked);

                response.SendChunked = false;
                Assert.False(response.SendChunked);
            }

            string clientResponse = GetClientResponse(143);
            Assert.DoesNotContain("Transfer-Encoding", clientResponse);
        }

        [Theory]
        [InlineData(true, 120)]
        [InlineData(false, 139)]
        public async Task KeepAlive_GetSet_ReturnsExpected(bool keepAlive, int expectedNumberOfBytes)
        {
            HttpListenerResponse response = await GetResponse();
            try
            {
                Assert.True(response.KeepAlive);
                Assert.Null(response.Headers[HttpResponseHeader.Connection]);
                Assert.Null(response.Headers[HttpResponseHeader.KeepAlive]);

                response.KeepAlive = keepAlive;
                Assert.Equal(keepAlive, response.KeepAlive);
                Assert.Null(response.Headers[HttpResponseHeader.Connection]);
                Assert.Null(response.Headers[HttpResponseHeader.KeepAlive]);
            }
            finally
            {
                response.Close();
            }

            // The Connection: close header should be added to the list of headers if KeepAlive == false.
            Assert.Equal(keepAlive ? null : "close", response.Headers[HttpResponseHeader.Connection]);
            Assert.Null(response.Headers[HttpResponseHeader.KeepAlive]);

            string clientResponse = GetClientResponse(expectedNumberOfBytes);
            if (keepAlive)
            {
                Assert.DoesNotContain("Connection", clientResponse);
            }
            else
            {
                Assert.Contains("\r\nConnection: close\r\n", clientResponse);
            }
        }
        
        [Fact]
        public async Task KeepAlive_SetDisposed_ThrowsObjectDisposedException()
        {
            HttpListenerResponse response = await GetResponse();
            response.Close();

            Assert.Null(response.Headers[HttpResponseHeader.Connection]);
            Assert.Null(response.Headers[HttpResponseHeader.KeepAlive]);

            Assert.Throws<ObjectDisposedException>(() => response.KeepAlive = false);
            Assert.True(response.KeepAlive);

            string clientResponse = GetClientResponse(120);
            Assert.DoesNotContain("Connection", clientResponse);
        }
        
        [Fact]
        public async Task KeepAlive_SetAfterHeadersSent_DoesNothing()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);

                response.KeepAlive = true;
                Assert.True(response.KeepAlive);
            }

            string clientResponse = GetClientResponse(111);
            Assert.DoesNotContain("Transfer-Encoding", clientResponse);
        }
        
        [Fact]
        public async Task KeepAlive_NoBoundaryAndRequestHttpRequestVersionMinorIsZero_SetsToFalseWhenSendingHeaders()
        {
            using (HttpListenerResponse response = await GetResponse("1.0"))
            {
                Assert.True(response.KeepAlive);

                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);
                Assert.False(response.KeepAlive);
            }

            string clientResponse = GetClientResponse(111);
            Assert.DoesNotContain("Transfer-Encoding", clientResponse);
        }
        
        [Fact]
        public async Task KeepAlive_ContentLengthBoundaryAndRequestHttpVersionMinorIsZero_DoesNotChangeWhenSendingHeaders()
        {
            using (HttpListenerResponse response = await GetResponse("1.0"))
            {
                Assert.True(response.KeepAlive);
                Assert.Null(response.Headers[HttpResponseHeader.KeepAlive]);

                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);
                Assert.True(response.KeepAlive);
                Assert.Equal("true", response.Headers[HttpResponseHeader.KeepAlive]);
            }

            string clientResponse = GetClientResponse(148);
            Assert.DoesNotContain("Transfer-Encoding", clientResponse);
        }

        [Theory]
        [InlineData(0, 106)]
        [InlineData(10, 117)]
        public async Task ContentLength64_GetSet_ReturnsExpected(int contentLength64, int expectedNumberOfBytes)
        {
            HttpListenerResponse response = await GetResponse();
            try
            {
                Assert.Equal(0, response.ContentLength64);
                response.SendChunked = true;

                response.ContentLength64 = contentLength64;
                Assert.Equal(contentLength64, response.ContentLength64);
                Assert.False(response.SendChunked);

                response.OutputStream.Write(new byte[contentLength64], 0, contentLength64);
            }
            finally
            {
                response.Close();
            }

            // The "Content-Length: contentLength64" header should be added to the list of headers if there is a Content-Length specified.
            Assert.Equal(contentLength64.ToString(), response.Headers[HttpResponseHeader.ContentLength]);

            string clientResponse = GetClientResponse(expectedNumberOfBytes);
            Assert.DoesNotContain("Transfer-Encoding", clientResponse);
            Assert.Contains($"\r\nContent-Length: {contentLength64}\r\n", clientResponse);
        }

        [Theory]
        [InlineData(100, 0, 112)]
        [InlineData(101, 0, 123)]
        [InlineData(204, 0, 114)]
        [InlineData(205, 0, 117)]
        [InlineData(304, 0, 116)]
        [InlineData(200, -1, 120)]
        public async Task ContentLength64_NotSetAndGetAfterSendingHeaders_ReturnValueDependsOnStatusCode(int statusCode, int expectedContentLength64, int expectedNumberOfBytes)
        {
            HttpListenerResponse response = await GetResponse();
            response.StatusCode = statusCode;
            response.Close();

            Assert.Equal(expectedContentLength64, response.ContentLength64);

            string clientResponse = GetClientResponse(expectedNumberOfBytes);
            if (expectedContentLength64 == -1)
            {
                Assert.DoesNotContain("Content-Length", clientResponse);
                Assert.Contains("\r\nTransfer-Encoding: chunked\r\n", clientResponse);
            }
            else
            {
                Assert.Contains($"\r\nContent-Length: {expectedContentLength64}\r\n", clientResponse);
                Assert.DoesNotContain("Transfer-Encoding", clientResponse);
            }
        }

        [Fact]
        public async Task ContentLength64_SetDisposed_ThrowsObjectDisposedException()
        {
            HttpListenerResponse response = await GetResponse();
            Assert.Equal(0, response.ContentLength64);
            response.Close();

            Assert.Throws<ObjectDisposedException>(() => response.ContentLength64 = 10);
            Assert.Equal(-1, response.ContentLength64);

            string clientResponse = GetClientResponse(120);
            Assert.DoesNotContain("Content-Length", clientResponse);
        }

        [Fact]
        public async Task ContentLength64_SetAfterHeadersSent_ThrowsInvalidOperationException()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.ContentLength64 = SimpleMessage.Length;
                response.OutputStream.Write(SimpleMessage, 0, SimpleMessage.Length);
                
                Assert.Throws<InvalidOperationException>(() => response.ContentLength64 = 10);
                Assert.Equal(SimpleMessage.Length, response.ContentLength64);
            }

            string clientResponse = GetClientResponse(111);
            Assert.DoesNotContain("Transfer-Encoding", clientResponse);
        }

        [Fact]
        public async Task ContentLength64_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => response.ContentLength64 = -1);
                Assert.Equal(0, response.ContentLength64);
            }

            string clientResponse = GetClientResponse(120);
            Assert.Contains("\r\nTransfer-Encoding: chunked\r\n", clientResponse);
            Assert.DoesNotContain("Content-Length", clientResponse);
        }

        public static IEnumerable<object[]> ProtocolVersion_Set_TestData()
        {
            yield return new object[] { new Version(1, 0) };
            yield return new object[] { new Version(1, 1) };

            // Build and Revision are ignored.
            yield return new object[] { new Version(1, 1, 2) };
            yield return new object[] { new Version(1, 1, 2, 3) };
        }

        [Theory]
        [MemberData(nameof(ProtocolVersion_Set_TestData))]
        public async Task ProtocolVersion_SetValid_ReturnsExpected(Version version)
        {
            Version expectedVersion = new Version(version.Major, version.Minor);

            using (HttpListenerResponse response = await GetResponse())
            {
                response.ProtocolVersion = version;
                Assert.Equal(expectedVersion, response.ProtocolVersion);
            }

            string clientResponse = GetClientResponse(120);
            Assert.StartsWith("HTTP/1.1 200 OK\r\n", clientResponse);
        }

        [Fact]
        public async Task ProtocolVersion_SetNull_ThrowsArgumentNullException()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                AssertExtensions.Throws<ArgumentNullException>("value", () => response.ProtocolVersion = null);
                Assert.Equal(new Version(1, 1), response.ProtocolVersion);
            }
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        [InlineData(2, 0)]
        public async Task ProtocolVersion_SetInvalid_ThrowsArgumentNullException(int major, int minor)
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                AssertExtensions.Throws<ArgumentException>("value", () => response.ProtocolVersion = new Version(major, minor));
                Assert.Equal(new Version(1, 1), response.ProtocolVersion);
            }
        }

        [Fact]
        public async Task Headers_GetSet_ReturnsExpected()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                Assert.Empty(response.Headers);
                response.Headers.Add("OldName", "OldValue");

                var headers = new WebHeaderCollection
                {
                    { "Name1", "Value1" },
                    { "Name2", "Value2" },
                    { "Name3", "" }
                };
                response.Headers = headers;
                Assert.Equal(headers, response.Headers);
                Assert.NotSame(headers, response.Headers);
            }

            string clientResponse = GetClientResponse(159);
            Assert.Contains("\r\nName1: Value1\r\nName2: Value2\r\nName3: \r\n", clientResponse);
        }

        [Fact]
        public async Task Headers_SetCollectionWithRequestHeaders_Works()
        {
            HttpListenerResponse response = await GetResponse();
            response.Headers.Add("name", "value");

            var headers = new WebHeaderCollection
            {
                { HttpRequestHeader.Accept, "value" }
            };
            response.Headers = headers;
            Assert.Equal("value", response.Headers["accept"]);
        }

        [Theory]
        [InlineData("Transfer-Encoding")]
        [ActiveIssue(22057, TargetFrameworkMonikers.Netcoreapp)]
        public async Task Headers_SetRestricted_ThrowsArgumentException(string name)
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentException>("name", () => response.Headers.Add(name, "value"));
            AssertExtensions.Throws<ArgumentException>("name", () => response.Headers.Add($"{name}:value"));
        }

        [Fact]
        public async Task Headers_SetNull_ThrowsNullReferenceException()
        {
            using (HttpListenerResponse response = await GetResponse())
            {
                response.Headers.Add("OldName", "OldValue");

                Assert.Throws<NullReferenceException>(() => response.Headers = null);
                Assert.Equal(0, response.Headers.Count);
            }
        }

        [Fact]
        [ActiveIssue(22057, TargetFrameworkMonikers.Netcoreapp)]
        public async Task Headers_SetLongName_ThrowsArgumentOutOfRangeException()
        {
            HttpListenerResponse response = await GetResponse();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => response.Headers["name"] = s_longString);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => response.Headers.Set("name", s_longString));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => response.Headers.Add("name", s_longString));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => response.Headers.Add($"name:{s_longString}"));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => response.Headers.Add(HttpResponseHeader.Age, s_longString));
        }

        [Fact]
        [ActiveIssue(22057, TargetFrameworkMonikers.Netcoreapp)]
        public async Task Headers_SetRequestHeader_ThrowsInvalidOperationException()
        {
            HttpListenerResponse response = await GetResponse();
            Assert.Throws<InvalidOperationException>(() => response.Headers[HttpRequestHeader.Accept]);
            Assert.Throws<InvalidOperationException>(() => response.Headers[HttpRequestHeader.Accept] = "value");
            Assert.Throws<InvalidOperationException>(() => response.Headers.Set(HttpRequestHeader.Accept, "value"));
            Assert.Throws<InvalidOperationException>(() => response.Headers.Add(HttpRequestHeader.Accept, "value"));
            Assert.Throws<InvalidOperationException>(() => response.Headers.Remove(HttpRequestHeader.Accept));
        }
    }
}
