// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Tests
{
    public class FtpWebRequestTest
    {
        public class FtpExecutionMode
        {
            public readonly bool UseSsl;
            public readonly bool UsePassive;
            public readonly bool UseAsync;
            public readonly bool UseOldStyleAsync;

            public FtpExecutionMode(bool useSsl, bool usePassive, bool useAsync, bool useOldStyleAsync)
            {
                UseSsl = useSsl;
                UsePassive = usePassive;
                UseAsync = useAsync;
                UseOldStyleAsync = useOldStyleAsync;
            }
        }

        [Fact]
        public void Ctor_VerifyDefaults_Success()
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://foo.com/bar");
            Assert.Equal(request.CachePolicy, WebRequest.DefaultCachePolicy);
            Assert.NotNull(request.ClientCertificates);
            Assert.Equal(0, request.ClientCertificates.Count);
            Assert.False(request.EnableSsl);
            Assert.NotNull(request.Headers);
            Assert.Equal(0, request.Headers.Count);
            Assert.True(request.KeepAlive);
            Assert.Equal(request.Method, WebRequestMethods.Ftp.DownloadFile);
            Assert.Null(request.Proxy);
            Assert.Equal(request.ReadWriteTimeout, 5 * 60 * 1000);
            Assert.Null(request.RenameTo);
            Assert.Equal("ftp", request.RequestUri.Scheme);
            Assert.Equal("foo.com", request.RequestUri.Host);
            Assert.Equal(21, request.RequestUri.Port);
            Assert.True(request.RequestUri.IsDefaultPort);
            Assert.Equal("/bar", request.RequestUri.AbsolutePath);
            Assert.Equal(request.ServicePoint, ServicePointManager.FindServicePoint(new Uri("ftp://foo.com/bar")));
            Assert.Equal(100000, request.Timeout);
            Assert.True(request.UseBinary);
            Assert.True(request.UsePassive);
        }

        [OuterLoop]
        [Fact]
        public void GetResponse_ServerNameNotInDns_ThrowsWebException()
        {
            string serverUrl = string.Format("ftp://www.{0}.com/", Guid.NewGuid().ToString());
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUrl);
            Assert.Throws<WebException>(() => request.GetResponse());
        }

        [OuterLoop]
        [Fact]
        public void GetResponse_ConnectFailure_ThrowsWebException()
        {
            // This is an invalid IP address, so we should fail to connect.
            string serverUrl = "ftp://192.0.2.1/";
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverUrl);
            WebException ex = Assert.Throws<WebException>(() => request.GetResponse());
            Assert.Equal(WebExceptionStatus.ConnectFailure, ex.Status);
        }

        private static bool LocalServerAvailable => (Environment.GetEnvironmentVariable("USE_LOCAL_FTP_SERVER") != null);

        private const string absoluteUri = "ftp://localhost/";

        private static readonly byte[] helloWorldBytes = Encoding.UTF8.GetBytes("Hello world");
        private static readonly byte[] largeFileBytes = Enumerable.Range(0, 10 * 1024 * 1024).Select((i) => (byte)(i % 256)).ToArray();

        [ConditionalTheory(nameof(LocalServerAvailable))]
        [MemberData(nameof(Modes))]
        public void Ftp_CreateAndDelete(FtpExecutionMode mode)
        {
            string uri = absoluteUri + Guid.NewGuid().ToString();

            DoUpload(mode, WebRequestMethods.Ftp.UploadFile, uri, helloWorldBytes);
            byte[] responseBytes = DoDownload(mode, WebRequestMethods.Ftp.DownloadFile, uri);

            Assert.True(responseBytes.SequenceEqual(helloWorldBytes));

            DoCommand(mode, WebRequestMethods.Ftp.DeleteFile, uri);

            Assert.False(FileExists(mode, uri));
        }

        [ConditionalTheory(nameof(LocalServerAvailable))]
        [MemberData(nameof(Modes))]
        public void Ftp_LargeFile(FtpExecutionMode mode)
        {
            string uri = absoluteUri + Guid.NewGuid().ToString();

            DoUpload(mode, WebRequestMethods.Ftp.UploadFile, uri, largeFileBytes);
            byte[] responseBytes = DoDownload(mode, WebRequestMethods.Ftp.DownloadFile, uri);

            Assert.True(responseBytes.SequenceEqual(largeFileBytes));

            DoCommand(mode, WebRequestMethods.Ftp.DeleteFile, uri);

            Assert.False(FileExists(mode, uri));
        }

        [ConditionalTheory(nameof(LocalServerAvailable))]
        [MemberData(nameof(Modes))]
        public void Ftp_AppendFile(FtpExecutionMode mode)
        {
            string uri = absoluteUri + Guid.NewGuid().ToString();

            DoUpload(mode, WebRequestMethods.Ftp.UploadFile, uri, helloWorldBytes);
            DoUpload(mode, WebRequestMethods.Ftp.AppendFile, uri, helloWorldBytes);

            byte[] responseBytes = DoDownload(mode, WebRequestMethods.Ftp.DownloadFile, uri);

            Assert.True(responseBytes.SequenceEqual(helloWorldBytes.Concat(helloWorldBytes)));

            DoCommand(mode, WebRequestMethods.Ftp.DeleteFile, uri);

            Assert.False(FileExists(mode, uri));
        }

        [ConditionalTheory(nameof(LocalServerAvailable))]
        [MemberData(nameof(Modes))]
        public void Ftp_RenameFile(FtpExecutionMode mode)
        {
            string uri = absoluteUri + Guid.NewGuid().ToString();
            string renameTo = Guid.NewGuid().ToString();
            string newUri = absoluteUri + renameTo;

            DoUpload(mode, WebRequestMethods.Ftp.UploadFile, uri, helloWorldBytes);
            byte[] responseBytes = DoDownload(mode, WebRequestMethods.Ftp.DownloadFile, uri);

            Assert.True(responseBytes.SequenceEqual(helloWorldBytes));

            DoCommand(mode, WebRequestMethods.Ftp.Rename, uri, renameTo);

            Assert.False(FileExists(mode, uri));

            responseBytes = DoDownload(mode, WebRequestMethods.Ftp.DownloadFile, newUri);

            Assert.True(responseBytes.SequenceEqual(helloWorldBytes));

            DoCommand(mode, WebRequestMethods.Ftp.DeleteFile, newUri);

            Assert.False(FileExists(mode, newUri));
        }

        [ConditionalTheory(nameof(LocalServerAvailable))]
        [MemberData(nameof(Modes))]
        public void Ftp_MakeAndRemoveDir_Success(FtpExecutionMode mode)
        {
            string dir = absoluteUri + Guid.NewGuid().ToString() + "/";

            DoCommand(mode, WebRequestMethods.Ftp.MakeDirectory, dir);

            Assert.True(DirExists(mode, dir));

            DoCommand(mode, WebRequestMethods.Ftp.RemoveDirectory, dir);

            Assert.False(DirExists(mode, dir));
        }

        [ConditionalTheory(nameof(LocalServerAvailable))]
        [MemberData(nameof(Modes))]
        public void Ftp_RenameFileSubDir_Success(FtpExecutionMode mode)
        {
            string dir = absoluteUri + Guid.NewGuid().ToString() + "/";
            string file = dir + "CreatedFile.txt";
            string renamedFileName = "RenamedFile.txt";
            string renamedFile = dir + "RenamedFile.txt";

            // Create
            DoCommand(mode, WebRequestMethods.Ftp.MakeDirectory, dir);
            DoUpload(mode, WebRequestMethods.Ftp.UploadFile, file, helloWorldBytes);

            Assert.True(DirExists(mode, dir));
            Assert.True(FileExists(mode, file));

            // Rename
            DoCommand(mode, WebRequestMethods.Ftp.Rename, file, renamedFileName);

            Assert.False(FileExists(mode, file));
            Assert.True(FileExists(mode, renamedFile));

            // Cleanup
            DoCommand(mode, WebRequestMethods.Ftp.DeleteFile, renamedFile);
            DoCommand(mode, WebRequestMethods.Ftp.RemoveDirectory, dir);

            Assert.False(FileExists(mode, renamedFile));
            Assert.False(DirExists(mode, dir));
        }

        private static async Task<MemoryStream> DoAsync(FtpWebRequest request, MemoryStream requestBody)
        {
            if (requestBody != null)
            {
                Stream requestStream = await request.GetRequestStreamAsync();
                await requestBody.CopyToAsync(requestStream);
                requestStream.Close();
            }

            MemoryStream responseBody = new MemoryStream();
            FtpWebResponse response = (FtpWebResponse)await request.GetResponseAsync();
            await response.GetResponseStream().CopyToAsync(responseBody);
            response.Close();

            return responseBody;
        }

        private static MemoryStream DoOldStyleAsync(FtpWebRequest request, MemoryStream requestBody)
        {
            if (requestBody != null)
            {
                IAsyncResult ar = request.BeginGetRequestStream(null, null);
                ar.AsyncWaitHandle.WaitOne();
                Stream requestStream = request.EndGetRequestStream(ar);
                requestBody.CopyTo(requestStream);
                requestStream.Close();
            }

            IAsyncResult ar2 = request.BeginGetResponse(null, null);
            ar2.AsyncWaitHandle.WaitOne();
            FtpWebResponse response = (FtpWebResponse)request.EndGetResponse(ar2);

            MemoryStream responseBody = new MemoryStream();
            response.GetResponseStream().CopyTo(responseBody);
            response.Close();

            return responseBody;
        }

        private static MemoryStream DoSync(FtpWebRequest request, MemoryStream requestBody)
        {
            if (requestBody != null)
            {
                Stream requestStream = request.GetRequestStream();
                requestBody.CopyTo(requestStream);
                requestStream.Close();
            }

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            MemoryStream responseBody = new MemoryStream();
            response.GetResponseStream().CopyTo(responseBody);
            response.Close();

            return responseBody;
        }

        private static byte[] Do(FtpExecutionMode mode, string method, string uri, byte[] requestBody, string renameTo = null)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(uri);

            request.EnableSsl = mode.UseSsl;
            request.UsePassive = mode.UsePassive;

            request.Method = method;
            if (renameTo != null)
            {
                request.RenameTo = renameTo;
            }

            MemoryStream requestMemStream = null;
            if (requestBody != null)
            {
                requestMemStream = new MemoryStream(requestBody);
            }

            if (mode.UseAsync)
            {
                try
                {
                    Task<MemoryStream> t = DoAsync(request, requestMemStream);
                    return t.Result.ToArray();
                }
                catch (AggregateException e)
                {
                    throw e.InnerException;
                }
            }
            else if (mode.UseOldStyleAsync)
            {
                return DoOldStyleAsync(request, requestMemStream).ToArray();
            }
            else
            {
                return DoSync(request, requestMemStream).ToArray();
            }
        }

        private static byte[] DoDownload(FtpExecutionMode mode, string method, string uri)
        {
            return Do(mode, method, uri, null);
        }

        private static void DoUpload(FtpExecutionMode mode, string method, string uri, byte[] requestBody)
        {
            byte[] responseBody = Do(mode, method, uri, requestBody);

            // Should be no response
            Assert.Equal(0, responseBody.Length);
        }

        private static void DoCommand(FtpExecutionMode mode, string method, string uri, string renameTo = null)
        {
            byte[] responseBody = Do(mode, method, uri, null, renameTo);

            // Should be no response
            Assert.Equal(0, responseBody.Length);
        }

        public static List<object[]> Modes = new List<object[]>
        {
            new object[] { new FtpExecutionMode(false, false, false, false) },
            new object[] { new FtpExecutionMode(false, false, true, false) },
            new object[] { new FtpExecutionMode(false, false, false, true) },

            new object[] { new FtpExecutionMode(true, false, false, false) },
            new object[] { new FtpExecutionMode(true, false, true, false) },
            new object[] { new FtpExecutionMode(true, false, false, true) },

            new object[] { new FtpExecutionMode(false, true, false, false) },
            new object[] { new FtpExecutionMode(false, true, true, false) },
            new object[] { new FtpExecutionMode(false, true, false, true) },

            new object[] { new FtpExecutionMode(true, true, false, false) },
            new object[] { new FtpExecutionMode(true, true, true, false) },
            new object[] { new FtpExecutionMode(true, true, false, true) },
        };

        private static bool DirExists(FtpExecutionMode mode, string dir)
        {
            try
            {
                DoDownload(mode, WebRequestMethods.Ftp.ListDirectory, dir);
                return true;
            }
            catch (WebException) { }

            return false;
        }

        private static bool FileExists(FtpExecutionMode mode, string file)
        {
            try
            {
                DoDownload(mode, WebRequestMethods.Ftp.DownloadFile, file);
                return true;
            }
            catch (WebException) { }

            return false;
        }
    }
}
