// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public static class TestHelper
    {
        public static TimeSpan PassingTestTimeout => TimeSpan.FromMilliseconds(PassingTestTimeoutMilliseconds);
        public const int PassingTestTimeoutMilliseconds = 60 * 1000;

        public static bool JsonMessageContainsKeyValue(string message, string key, string value)
        {
            // Deal with JSON encoding of '\' and '"' in value
            value = value.Replace("\\", "\\\\").Replace("\"", "\\\"");

            // In HTTP2, all header names are in lowercase. So accept either the original header name or the lowercase version.
            return message.Contains($"\"{key}\": \"{value}\"") ||
                message.Contains($"\"{key.ToLowerInvariant()}\": \"{value}\"");
        }

        public static bool JsonMessageContainsKey(string message, string key)
        {
            return JsonMessageContainsKeyValue(message, key, "");
        }

        public static void VerifyResponseBody(
            string responseContent,
            byte[] expectedMD5Hash,
            bool chunkedUpload,
            string requestBody)
        {
            // [ActiveIssue("https://github.com/dotnet/runtime/issues/37669", TestPlatforms.Browser)]
            if (!PlatformDetection.IsBrowser)
            {
                // Verify that response body from the server was corrected received by comparing MD5 hash.
                byte[] actualMD5Hash = ComputeMD5Hash(responseContent);
                Assert.Equal(expectedMD5Hash, actualMD5Hash);
            }

            // Verify upload semantics: 'Content-Length' vs. 'Transfer-Encoding: chunked'.
            if (requestBody != null)
            {
                bool requestUsedContentLengthUpload =
                    JsonMessageContainsKeyValue(responseContent, "Content-Length", requestBody.Length.ToString());
                bool requestUsedChunkedUpload =
                    JsonMessageContainsKeyValue(responseContent, "Transfer-Encoding", "chunked");
                if (requestBody.Length > 0)
                {
                    Assert.NotEqual(requestUsedContentLengthUpload, requestUsedChunkedUpload);
                    Assert.Equal(chunkedUpload, requestUsedChunkedUpload);
                    Assert.Equal(!chunkedUpload, requestUsedContentLengthUpload);
                }

                // Verify that request body content was correctly sent to server.
                Assert.True(JsonMessageContainsKeyValue(responseContent, "BodyContent", requestBody), "Valid request body");
            }
        }

        public static void VerifyRequestMethod(HttpResponseMessage response, string expectedMethod)
        {
           IEnumerable<string> values = response.Headers.GetValues("X-HttpRequest-Method");
           foreach (string value in values)
           {
               Assert.Equal(expectedMethod, value);
           }
        }

        public static byte[] ComputeMD5Hash(string data)
        {
            return ComputeMD5Hash(Encoding.UTF8.GetBytes(data));
        }

        public static byte[] ComputeMD5Hash(byte[] data)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(data);
            }
        }

        public static Task WhenAllCompletedOrAnyFailed(params Task[] tasks)
        {
            return TaskTimeoutExtensions.WhenAllOrAnyFailed(tasks, PlatformDetection.IsArmProcess || PlatformDetection.IsArm64Process ? PassingTestTimeoutMilliseconds * 5 : PassingTestTimeoutMilliseconds);
        }

        public static Task WhenAllCompletedOrAnyFailedWithTimeout(int timeoutInMilliseconds, params Task[] tasks)
        {
            return TaskTimeoutExtensions.WhenAllOrAnyFailed(tasks, timeoutInMilliseconds);
        }

#if NET
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> AllowAllCertificates = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#else
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> AllowAllCertificates = (_, __, ___, ____) => true;
#endif

        public static byte[] GenerateRandomContent(int size)
        {
            byte[] data = new byte[size];
            new Random(42).NextBytes(data);
            return data;
        }

        public static X509Certificate2 CreateServerSelfSignedCertificate(string name = "localhost", bool expired = false)
        {
            using (RSA root = RSA.Create())
            {
                CertificateRequest req = new CertificateRequest(
                    $"CN={name}",
                    root,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                req.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
                req.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(req.PublicKey, false));
                req.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment, false));
                req.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                            new OidCollection()
                                {
                                    new Oid("1.3.6.1.5.5.7.3.1", null),
                                }, false));


                SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
                builder.AddDnsName(name);
                builder.AddIpAddress(IPAddress.Loopback);
                builder.AddIpAddress(IPAddress.IPv6Loopback);
                req.CertificateExtensions.Add(builder.Build());

                DateTimeOffset start = expired ? DateTimeOffset.UtcNow.AddMonths(-2) : DateTimeOffset.UtcNow.AddMinutes(-5);
                DateTimeOffset end = start.AddMonths(1);

                X509Certificate2 cert = req.CreateSelfSigned(start, end);
                if (PlatformDetection.IsWindows)
                {
                    using (cert)
                    {
                        return X509CertificateLoader.LoadPkcs12(cert.Export(X509ContentType.Pfx), (string?)null);
                    }
                }

                return cert;
            }
        }

        public static (X509Certificate2 Certificate, X509Certificate2 RootCertificate) CreateExpiredServerCertificate(string name)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using RSA rootKey = RSA.Create();
            var rootRequest = new CertificateRequest(
                $"CN={name} Test Root",
                rootKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            rootRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            rootRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));
            rootRequest.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(rootRequest.PublicKey, false));

            using X509Certificate2 rootWithPrivateKey = rootRequest.CreateSelfSigned(now.AddMonths(-3), now.AddYears(1));

            using RSA serverKey = RSA.Create();
            var serverRequest = new CertificateRequest(
                $"CN={name}",
                serverKey,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            serverRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            serverRequest.CertificateExtensions.Add(new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment,
                false));
            serverRequest.CertificateExtensions.Add(
                new X509EnhancedKeyUsageExtension(
                    new OidCollection { new Oid("1.3.6.1.5.5.7.3.1", null) },
                    false));

            SubjectAlternativeNameBuilder builder = new SubjectAlternativeNameBuilder();
            builder.AddDnsName(name);
            builder.AddIpAddress(IPAddress.Loopback);
            builder.AddIpAddress(IPAddress.IPv6Loopback);
            serverRequest.CertificateExtensions.Add(builder.Build());

            byte[] serialNumber = new byte[8];
            using (RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create())
            {
                randomNumberGenerator.GetBytes(serialNumber);
            }
            serialNumber[0] &= 0x7f;
            serialNumber[0] |= 0x01;

            using X509Certificate2 serverWithoutPrivateKey = serverRequest.Create(
                rootWithPrivateKey,
                now.AddMonths(-2),
                now.AddMonths(-1),
                serialNumber);

            X509Certificate2 certificateWithPrivateKey = serverWithoutPrivateKey.CopyWithPrivateKey(serverKey);
            X509Certificate2 certificate;
            if (PlatformDetection.IsWindows)
            {
                using (certificateWithPrivateKey)
                {
                    certificate = X509CertificateLoader.LoadPkcs12(
                        certificateWithPrivateKey.Export(X509ContentType.Pfx),
                        (string?)null);
                }
            }
            else
            {
                certificate = certificateWithPrivateKey;
            }

            return (certificate, X509CertificateLoader.LoadCertificate(rootWithPrivateKey.RawData));
        }

#if NET
        public static SocketsHttpHandler CreateSocketsHttpHandler(bool allowAllCertificates = false)
        {
            var handler = new SocketsHttpHandler();

            // Browser doesn't support ServerCertificateCustomValidationCallback
            if (allowAllCertificates && PlatformDetection.IsNotBrowser)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
            }

            return handler;
        }
#endif
    }
}
