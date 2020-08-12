// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Net.Test.Common;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Functional.Tests
{
    public static class TestHelper
    {
        public static int PassingTestTimeoutMilliseconds => 60 * 1000;
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
            // Verify that response body from the server was corrected received by comparing MD5 hash.
            byte[] actualMD5Hash = ComputeMD5Hash(responseContent);
            Assert.Equal(expectedMD5Hash, actualMD5Hash);

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

#if NETCOREAPP
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> AllowAllCertificates = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
#else
        public static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> AllowAllCertificates = (_, __, ___, ____) => true;
#endif

        public static IPAddress GetIPv6LinkLocalAddress() =>
            NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(i => !i.Description.StartsWith("PANGP Virtual Ethernet"))    // This is a VPN adapter, but is reported as a regular Ethernet interface with
                                                                                    // a valid link-local address, but the link-local address doesn't actually work.
                                                                                    // So just manually filter it out.
                .SelectMany(i => i.GetIPProperties().UnicastAddresses)
                .Select(a => a.Address)
                .Where(a => a.IsIPv6LinkLocal)
                .FirstOrDefault();

        public static byte[] GenerateRandomContent(int size)
        {
            byte[] data = new byte[size];
            new Random(42).NextBytes(data);
            return data;
        }
    }
}
