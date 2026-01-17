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
    public class FtpWebRequestAuthenticationTest
    {
        private readonly ITestOutputHelper _output;
        private const string FtpServerUrl = "apacheweb.linux.contoso.com";
        private const string FtpUsername = "ftpuser";
        private const string FtpPassword = "ftppass";

        public FtpWebRequestAuthenticationTest(ITestOutputHelper output)
        {
            _output = output;
            
            // Set up certificate validation callback to accept self-signed certificates in test environment
#pragma warning disable SYSLIB0014 // ServicePointManager is obsolete
            ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
#pragma warning restore SYSLIB0014
        }

        private static bool ValidateServerCertificate(
            object sender,
            X509Certificate? certificate,
            X509Chain? chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // In the enterprise test environment, accept self-signed certificates
            // This is safe because we're in a controlled test environment
            return true;
        }

        [ConditionalFact(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
#pragma warning disable SYSLIB0014 // WebRequest, FtpWebRequest, and related types are obsolete
        public void FtpUpload_NoSsl_Baseline()
        {
            string fileName = $"test_{Guid.NewGuid()}.txt";
            string url = $"ftp://{FtpUsername}:{FtpPassword}@{FtpServerUrl}/ftp/{fileName}";
            byte[] data = Encoding.UTF8.GetBytes("Test data for FTP upload without SSL");

            _output.WriteLine($"Testing baseline FTP upload without SSL to: {url}");

            // Upload file without SSL (baseline test)
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.EnableSsl = false;
            request.Credentials = new NetworkCredential(FtpUsername, FtpPassword);

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
                deleteRequest.EnableSsl = false;
                deleteRequest.Credentials = new NetworkCredential(FtpUsername, FtpPassword);
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
#pragma warning restore SYSLIB0014

        [ConditionalFact(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
#pragma warning disable SYSLIB0014 // WebRequest, FtpWebRequest, and related types are obsolete
        public async Task FtpUploadWithSsl_Success()
        {
            string fileName = $"test_{Guid.NewGuid()}.txt";
            string url = $"ftp://{FtpUsername}:{FtpPassword}@{FtpServerUrl}/ftp/{fileName}";
            byte[] data = Encoding.UTF8.GetBytes("Test data for FTP/SSL upload");

            _output.WriteLine($"Testing FTP upload with SSL to: {url}");

            // Upload file with SSL
            FtpWebRequest uploadRequest = (FtpWebRequest)WebRequest.Create(url);
            uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;
            uploadRequest.EnableSsl = true;
            uploadRequest.Credentials = new NetworkCredential(FtpUsername, FtpPassword);

            using (Stream requestStream = await uploadRequest.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(data, 0, data.Length);
            }

            using (FtpWebResponse response = (FtpWebResponse)await uploadRequest.GetResponseAsync())
            {
                _output.WriteLine($"Upload response: {response.StatusCode} - {response.StatusDescription}");
                Assert.Equal(FtpStatusCode.ClosingData, response.StatusCode);
            }

            // Cleanup - delete the uploaded file
            try
            {
                FtpWebRequest deleteRequest = (FtpWebRequest)WebRequest.Create(url);
                deleteRequest.Method = WebRequestMethods.Ftp.DeleteFile;
                deleteRequest.EnableSsl = true;
                deleteRequest.Credentials = new NetworkCredential(FtpUsername, FtpPassword);
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

        [ConditionalFact(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
#pragma warning disable SYSLIB0014 // WebRequest, FtpWebRequest, and related types are obsolete
        public async Task FtpDownloadWithSsl_Success()
        {
            string fileName = $"test_{Guid.NewGuid()}.txt";
            string url = $"ftp://{FtpUsername}:{FtpPassword}@{FtpServerUrl}/ftp/{fileName}";
            byte[] uploadData = Encoding.UTF8.GetBytes("Test data for FTP/SSL download");

            _output.WriteLine($"Testing FTP download with SSL from: {url}");

            // First upload a file
            FtpWebRequest uploadRequest = (FtpWebRequest)WebRequest.Create(url);
            uploadRequest.Method = WebRequestMethods.Ftp.UploadFile;
            uploadRequest.EnableSsl = true;
            uploadRequest.Credentials = new NetworkCredential(FtpUsername, FtpPassword);

            using (Stream requestStream = await uploadRequest.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(uploadData, 0, uploadData.Length);
            }

            using (FtpWebResponse uploadResponse = (FtpWebResponse)await uploadRequest.GetResponseAsync())
            {
                _output.WriteLine($"Upload response: {uploadResponse.StatusCode}");
            }

            // Now download it
            FtpWebRequest downloadRequest = (FtpWebRequest)WebRequest.Create(url);
            downloadRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            downloadRequest.EnableSsl = true;
            downloadRequest.Credentials = new NetworkCredential(FtpUsername, FtpPassword);

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
                deleteRequest.EnableSsl = true;
                deleteRequest.Credentials = new NetworkCredential(FtpUsername, FtpPassword);
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

        [ConditionalFact(typeof(EnterpriseTestConfiguration), nameof(EnterpriseTestConfiguration.Enabled))]
#pragma warning disable SYSLIB0014 // WebRequest, FtpWebRequest, and related types are obsolete
        public void FtpUploadWithSsl_StreamDisposal_NoProtocolViolation()
        {
            string fileName = $"test_{Guid.NewGuid()}.txt";
            string url = $"ftp://{FtpUsername}:{FtpPassword}@{FtpServerUrl}/ftp/{fileName}";
            byte[] data = Encoding.UTF8.GetBytes("Test data for stream disposal");

            _output.WriteLine($"Testing FTP upload stream disposal with SSL to: {url}");

            // This test specifically validates that stream disposal doesn't cause protocol violations
            // which was the issue reported in dotnet/runtime#123135
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.EnableSsl = true;
            request.Credentials = new NetworkCredential(FtpUsername, FtpPassword);

            // The exception "The underlying connection was closed: The server committed a protocol violation"
            // used to occur when the request stream was disposed
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            } // Stream disposal should not throw

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
                deleteRequest.EnableSsl = true;
                deleteRequest.Credentials = new NetworkCredential(FtpUsername, FtpPassword);
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
#pragma warning restore SYSLIB0014
    }
}
