// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    public class FileWebRequestTest
    {
        private readonly ITestOutputHelper _output;

        public FileWebRequestTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Ctor_VerifyDefaults_Success()
        {
            Uri uri = new Uri("file://somefilepath");
            FileWebRequest request = (FileWebRequest)WebRequest.Create(uri);
            Assert.Null(request.ContentType);
            Assert.Null(request.Credentials);
            Assert.NotNull(request.Headers);
            Assert.Equal(0, request.Headers.Count);
            Assert.Equal("GET", request.Method);
            Assert.Null(request.Proxy);
            Assert.Equal(uri, request.RequestUri);
        }

        [Fact]
        public void FileWebRequest_Properties_Roundtrips()
        {
            WebRequest request = WebRequest.Create("file://anything");

            request.ContentLength = 42;
            Assert.Equal(42, request.ContentLength);

            request.ContentType = "anything";
            Assert.Equal("anything", request.ContentType);

            request.Timeout = 42000;
            Assert.Equal(42000, request.Timeout);
        }

        [Fact]
        public void InvalidArguments_Throws()
        {
            WebRequest request = WebRequest.Create("file://anything");
            AssertExtensions.Throws<ArgumentException>("value", () => request.ContentLength = -1);
            AssertExtensions.Throws<ArgumentException>("value", () => request.Method = null);
            AssertExtensions.Throws<ArgumentException>("value", () => request.Method = "");
            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => request.Timeout = -2);
        }

        [Fact]
        public void GetRequestStream_MethodGet_ThrowsProtocolViolation()
        {
            WebRequest request = WebRequest.Create("file://anything");
            Assert.Throws<ProtocolViolationException>(() => request.BeginGetRequestStream(null, null));
        }

        [Fact]
        public void GetRequestResponseAfterAbort_Throws()
        {
            WebRequest request = WebRequest.Create("file://anything");
            request.Abort();
            Assert.Throws<WebException>(() => request.BeginGetRequestStream(null, null));
            Assert.Throws<WebException>(() => request.BeginGetResponse(null, null));
        }

        [Fact]
        public void UseDefaultCredentials_GetOrSet_Throws()
        {
            WebRequest request = WebRequest.Create("file://anything");
            Assert.Throws<NotSupportedException>(() => request.UseDefaultCredentials);
            Assert.Throws<NotSupportedException>(() => request.UseDefaultCredentials = true);
        }
    }

    public abstract class FileWebRequestTestBase
    {
        public abstract Task<WebResponse> GetResponseAsync(WebRequest request);
        public abstract Task<Stream> GetRequestStreamAsync(WebRequest request);

        [Fact]
        public async Task ReadFile_ContainsExpectedContent()
        {
            string path = Path.GetTempFileName();
            try
            {
                var data = new byte[1024 * 10];
                var random = new Random(42);
                random.NextBytes(data);

                File.WriteAllBytes(path, data);

                WebRequest request = WebRequest.Create("file://" + path);
                using (WebResponse response = await GetResponseAsync(request))
                {
                    Assert.Equal(data.Length, response.ContentLength);

                    Assert.Equal("application/octet-stream", response.ContentType);

                    Assert.True(response.SupportsHeaders);
                    Assert.NotNull(response.Headers);
                    Assert.Equal(new Uri("file://" + path), response.ResponseUri);

                    using (Stream s = response.GetResponseStream())
                    {
                        var target = new MemoryStream();
                        await s.CopyToAsync(target);
                        Assert.Equal(data, target.ToArray());
                    }
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task WriteFile_ContainsExpectedContent()
        {
            string path = Path.GetTempFileName();
            try
            {
                var data = new byte[1024 * 10];
                var random = new Random(42);
                random.NextBytes(data);

                var request = WebRequest.Create("file://" + path);
                request.Method = WebRequestMethods.File.UploadFile;

                using (Stream s = await GetRequestStreamAsync(request))
                {
                    await s.WriteAsync(data, 0, data.Length);
                }

                Assert.Equal(data, File.ReadAllBytes(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public async Task WriteThenReadFile_WriteAccessResultsInNullResponseStream()
        {
            string path = Path.GetTempFileName();
            try
            {
                var data = new byte[1024 * 10];
                var random = new Random(42);
                random.NextBytes(data);

                var request = WebRequest.Create("file://" + path);
                request.Method = WebRequestMethods.File.UploadFile;

                using (Stream s = await GetRequestStreamAsync(request))
                {
                    await s.WriteAsync(data, 0, data.Length);
                }

                using (WebResponse response = await GetResponseAsync(request))
                using (Stream s = response.GetResponseStream()) // will hand back a null stream
                {
                    Assert.Equal(0, s.Length);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        protected virtual bool EnableConcurrentReadWriteTests => true;

        [Fact]
        public async Task RequestAfterResponse_throws()
        {
            string path = Path.GetTempFileName();
            try
            {
                WebRequest request = WebRequest.Create("file://" + path);
                request.Method = WebRequestMethods.File.UploadFile;
                using (WebResponse response = await GetResponseAsync(request))
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => GetRequestStreamAsync(request));
                }
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BeginGetResponse_OnNonexistentFile_ShouldNotCrashApplication(bool? abortWithDelay)
        {
            FileWebRequest request = (FileWebRequest)WebRequest.Create("file://" + Path.GetRandomFileName());
            Task<WebResponse> responseTask = GetResponseAsync(request);
            if (abortWithDelay.HasValue)
            {
                if (abortWithDelay.Value)
                {
                    await Task.Delay(1);
                }
                request.Abort();
            }
            await Assert.ThrowsAsync<WebException>(() => responseTask);
        }
    }

    public abstract class AsyncFileWebRequestTestBase : FileWebRequestTestBase
    {
        [Fact]
        public async Task ConcurrentReadWrite_ResponseBlocksThenGetsNullStream()
        {
            string path = Path.GetTempFileName();
            try
            {
                var data = new byte[1024 * 10];
                var random = new Random(42);
                random.NextBytes(data);

                var request = WebRequest.Create("file://" + path);
                request.Method = WebRequestMethods.File.UploadFile;

                Task<Stream> requestStreamTask = GetRequestStreamAsync(request);
                Task<WebResponse> responseTask = GetResponseAsync(request);

                using (Stream s = await requestStreamTask)
                {
                    await s.WriteAsync(data, 0, data.Length);
                }

                using (WebResponse response = await responseTask)
                using (Stream s = response.GetResponseStream()) // will hand back a null stream
                {
                    Assert.Equal(0, s.Length);
                }
            }
            finally
            {
                File.Delete(path);
            }
        }
    }

    public sealed class SyncFileWebRequestTestBase : FileWebRequestTestBase
    {
        public override Task<WebResponse> GetResponseAsync(WebRequest request) => Task.Run(() => request.GetResponse());
        public override Task<Stream> GetRequestStreamAsync(WebRequest request) => Task.Run(() => request.GetRequestStream());
    }

    public sealed class BeginEndFileWebRequestTestBase : AsyncFileWebRequestTestBase
    {
        public override Task<WebResponse> GetResponseAsync(WebRequest request) =>
            Task.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, null);

        public override Task<Stream> GetRequestStreamAsync(WebRequest request) =>
            Task.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, null);
    }

    public sealed class TaskFileWebRequestTestBase : AsyncFileWebRequestTestBase
    {
        public override Task<WebResponse> GetResponseAsync(WebRequest request) => request.GetResponseAsync();

        public override Task<Stream> GetRequestStreamAsync(WebRequest request) => request.GetRequestStreamAsync();
    }
}
