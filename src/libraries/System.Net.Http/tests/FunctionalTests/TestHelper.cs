// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            // TODO (#5525): Align with the rest of tests w.r.t response parsing once the test server is finalized.
            // Currently not adding any new dependencies

            // Deal with JSON encoding of '\' and '"' in value
            value = value.Replace("\\", "\\\\").Replace("\"", "\\\"");

            // In HTTP2, all header names are in lowercase. So accept either the original header name or the lowercase version.
            return message.Contains($"\"{key}\": \"{value}\"") ||
                message.Contains($"\"{key.ToLowerInvariant()}\": \"{value}\"");
        }

        public static bool JsonMessageContainsKey(string message, string key)
        {
            // TODO (#5525): Align with the rest of tests w.r.t response parsing once the test server is finalized.
            // Currently not adding any new dependencies

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

#if netcoreapp
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

        public static void EnsureHttp2Feature(HttpClientHandler handler, bool useHttp2LoopbackServer = true)
        {
            // All .NET Core implementations of HttpClientHandler have HTTP/2 enabled by default except when using
            // SocketsHttpHandler. Right now, the HTTP/2 feature is disabled on SocketsHttpHandler unless certain
            // AppContext switches or environment variables are set. To help with testing, we can enable the HTTP/2
            // feature for a specific handler instance by using reflection.
            FieldInfo field_socketsHttpHandler = typeof(HttpClientHandler).GetField(
                "_socketsHttpHandler",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (field_socketsHttpHandler == null)
            {
                // Not using .NET Core implementation, i.e. could be .NET Framework or UAP.
                return;
            }

            object _socketsHttpHandler = field_socketsHttpHandler.GetValue(handler);
            if (_socketsHttpHandler == null)
            {
                // Not using SocketsHttpHandler, i.e. using WinHttpHandler or CurlHandler.
                return;
            }

            // Get HttpConnectionSettings object from SocketsHttpHandler.
            Type type_SocketsHttpHandler = typeof(HttpClientHandler).Assembly.GetType("System.Net.Http.SocketsHttpHandler");
            FieldInfo field_settings = type_SocketsHttpHandler.GetField(
                "_settings",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field_settings);
            object _settings = field_settings.GetValue(_socketsHttpHandler);
            Assert.NotNull(_settings);

            // Set _maxHttpVersion field to HTTP/2.0.
            Type type_HttpConnectionSettings = typeof(HttpClientHandler).Assembly.GetType("System.Net.Http.HttpConnectionSettings");
            FieldInfo field_maxHttpVersion = type_HttpConnectionSettings.GetField(
                "_maxHttpVersion",
                BindingFlags.NonPublic | BindingFlags.Instance);
            field_maxHttpVersion.SetValue(_settings, new Version(2, 0));

            if (useHttp2LoopbackServer && (!PlatformDetection.SupportsAlpn || Capability.Http2ForceUnencryptedLoopback()))
            {
                // Allow HTTP/2.0 via unencrypted socket if ALPN is not supported on platform.
                FieldInfo field_allowPlainHttp2 = type_HttpConnectionSettings.GetField(
                    "_allowUnencryptedHttp2",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                field_allowPlainHttp2.SetValue(_settings, true);
            }
        }

        public static bool NativeHandlerSupportsSslConfiguration()
        {
#if TargetsWindows
            return true;
#else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return false;
            }

            // For other Unix-based systems it's true if (and only if) the currect openssl backend
            // is used with libcurl.
            bool hasAnyOpenSsl =
                Interop.Http.GetSslVersionDescription()?.StartsWith(Interop.Http.OpenSslDescriptionPrefix, StringComparison.OrdinalIgnoreCase) ?? false;

            if (!hasAnyOpenSsl)
            {
                return false;
            }

            // We're on an OpenSSL-based system, with an OpenSSL backend.
            // Ask the product how it feels about this.
            Type interopHttp = typeof(HttpClient).Assembly.GetType("Interop+Http");
            PropertyInfo hasMatchingOpenSslVersion = interopHttp.GetProperty("HasMatchingOpenSslVersion", BindingFlags.Static | BindingFlags.NonPublic);
            return (bool)hasMatchingOpenSslVersion.GetValue(null);
#endif
        }

        public static byte[] GenerateRandomContent(int size)
        {
            byte[] data = new byte[size];
            new Random(42).NextBytes(data);
            return data;
        }
    }
}
