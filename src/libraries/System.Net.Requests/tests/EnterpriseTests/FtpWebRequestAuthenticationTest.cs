// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Test.Common;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Tests
{
    [ConditionalClass(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
    public class FtpWebRequestStreamDisposalTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly RemoteCertificateValidationCallback? _previousCallback;

        public FtpWebRequestStreamDisposalTest(ITestOutputHelper output)
        {
            _output = output;

            // Save and replace the global certificate validation callback to accept self-signed certificates
            // in the controlled test environment. We restore it in Dispose().
#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete
            _previousCallback = ServicePointManager.ServerCertificateValidationCallback;
            ServicePointManager.ServerCertificateValidationCallback = AcceptAnyCertificate;
#pragma warning restore SYSLIB0014
        }

        public void Dispose()
        {
#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete
            ServicePointManager.ServerCertificateValidationCallback = _previousCallback;
#pragma warning restore SYSLIB0014
        }

        private static bool AcceptAnyCertificate(
            object sender,
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors) => true;

        private static string GetFtpUrl(string fileName) =>
            $"ftp://{EnterpriseTestConfiguration.FtpServer}/ftp/{fileName}";

        [ConditionalTheory(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
        [InlineData(false)]
        [InlineData(true)]
#pragma warning disable SYSLIB0014 // WebRequest, FtpWebRequest, and related types are obsolete
        public void FtpUpload_StreamDisposal(bool useSsl)
        {
            string fileName = $"test_{Guid.NewGuid()}.txt";
            string url = GetFtpUrl(fileName);
            byte[] data = Encoding.UTF8.GetBytes($"Test data for stream disposal (SSL={useSsl})");

            _output.WriteLine($"Testing FTP upload stream disposal with SSL={useSsl}");

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.EnableSsl = useSsl;
            request.Credentials = EnterpriseTestConfiguration.FtpNetworkCredentials;

            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
            {
                _output.WriteLine($"Response: {response.StatusCode} - {response.StatusDescription}");
                Assert.Equal(FtpStatusCode.ClosingData, response.StatusCode);
            }

            // Cleanup
            try
            {
                FtpWebRequest deleteRequest = (FtpWebRequest)WebRequest.Create(url);
                deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                deleteRequest.EnableSsl = useSsl;
                deleteRequest.Credentials = EnterpriseTestConfiguration.FtpNetworkCredentials;
                using (FtpWebResponse deleteResponse = (FtpWebResponse)deleteRequest.GetResponse())
                {
                    _output.WriteLine($"Delete response: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Cleanup failed: {ex.Message}");
            }
        }

        [ConditionalTheory(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task FtpDownload_StreamDisposal(bool useSsl)
        {
            string fileName = $"test_{Guid.NewGuid()}.txt";
            string url = GetFtpUrl(fileName);
            byte[] uploadData = Encoding.UTF8.GetBytes($"Test data for download disposal (SSL={useSsl})");

            _output.WriteLine($"Testing FTP download stream disposal with SSL={useSsl}");

            // First upload a file
            FtpWebRequest uploadRequest = (FtpWebRequest)WebRequest.Create(url);
            uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;
            uploadRequest.EnableSsl = useSsl;
            uploadRequest.Credentials = EnterpriseTestConfiguration.FtpNetworkCredentials;

            using (Stream requestStream = await uploadRequest.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(uploadData, 0, uploadData.Length);
            }

            using (FtpWebResponse uploadResponse = (FtpWebResponse)await uploadRequest.GetResponseAsync())
            {
                _output.WriteLine($"Upload response: {uploadResponse.StatusCode}");
            }

            // Now download it and validate stream disposal
            FtpWebRequest downloadRequest = (FtpWebRequest)WebRequest.Create(url);
            downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            downloadRequest.EnableSsl = useSsl;
            downloadRequest.Credentials = EnterpriseTestConfiguration.FtpNetworkCredentials;

            using (FtpWebResponse response = (FtpWebResponse)await downloadRequest.GetResponseAsync())
            using (Stream responseStream = response.GetResponseStream())
            using (MemoryStream ms = new MemoryStream())
            {
                await responseStream.CopyToAsync(ms);
                byte[] downloadedData = ms.ToArray();
                string downloadedText = Encoding.UTF8.GetString(downloadedData);

                _output.WriteLine($"Downloaded {downloadedData.Length} bytes");
                _output.WriteLine($"Download response: {response.StatusCode} - {response.StatusDescription}");

                Assert.Equal(FtpStatusCode.ClosingData, response.StatusCode);
                Assert.Equal(uploadData.Length, downloadedData.Length);
                Assert.Equal(Encoding.UTF8.GetString(uploadData), downloadedText);
            }

            // Cleanup - delete the uploaded file
            try
            {
                FtpWebRequest deleteRequest = (FtpWebRequest)WebRequest.Create(url);
                deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                deleteRequest.EnableSsl = useSsl;
                deleteRequest.Credentials = EnterpriseTestConfiguration.FtpNetworkCredentials;
                using (FtpWebResponse deleteResponse = (FtpWebResponse)await deleteRequest.GetResponseAsync())
                {
                    _output.WriteLine($"Delete response: {deleteResponse.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Cleanup failed: {ex.Message}");
            }
        }
#pragma warning restore SYSLIB0014
    }
}
