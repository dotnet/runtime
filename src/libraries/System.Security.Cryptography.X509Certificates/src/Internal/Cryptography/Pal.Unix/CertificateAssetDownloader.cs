// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static class CertificateAssetDownloader
    {
        private static readonly Func<string, CancellationToken, byte[]>? s_downloadBytes = CreateDownloadBytesFunc();

        internal static X509Certificate2? DownloadCertificate(string uri, TimeSpan downloadTimeout)
        {
            byte[]? data = DownloadAsset(uri, downloadTimeout);

            if (data == null || data.Length == 0)
            {
                return null;
            }

            try
            {
                X509Certificate2 certificate = new X509Certificate2(data);
                certificate.ThrowIfInvalid();
                return certificate;
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        internal static SafeX509CrlHandle? DownloadCrl(string uri, TimeSpan downloadTimeout)
        {
            byte[]? data = DownloadAsset(uri, downloadTimeout);

            if (data == null)
            {
                return null;
            }

            // DER-encoded CRL seems to be the most common off of some random spot-checking, so try DER first.
            SafeX509CrlHandle handle = Interop.Crypto.DecodeX509Crl(data, data.Length);

            if (!handle.IsInvalid)
            {
                return handle;
            }

            using (SafeBioHandle bio = Interop.Crypto.CreateMemoryBio())
            {
                Interop.Crypto.CheckValidOpenSslHandle(bio);

                Interop.Crypto.BioWrite(bio, data, data.Length);

                handle = Interop.Crypto.PemReadBioX509Crl(bio);

                // DecodeX509Crl failed, so we need to clear its error.
                // If PemReadBioX509Crl failed, clear that too.
                Interop.Crypto.ErrClearError();

                if (!handle.IsInvalid)
                {
                    return handle;
                }
            }

            return null;
        }

        internal static SafeOcspResponseHandle? DownloadOcspGet(string uri, TimeSpan downloadTimeout)
        {
            byte[]? data = DownloadAsset(uri, downloadTimeout);

            if (data == null)
            {
                return null;
            }

            // https://tools.ietf.org/html/rfc6960#appendix-A.2 says that the response is the DER-encoded
            // response, so no rebuffering to interpret PEM is required.
            SafeOcspResponseHandle resp = Interop.Crypto.DecodeOcspResponse(data);

            if (resp.IsInvalid)
            {
                // We're not going to report this error to a user, so clear it
                // (to avoid tainting future exceptions)
                Interop.Crypto.ErrClearError();
            }

            return resp;
        }

        private static byte[]? DownloadAsset(string uri, TimeSpan downloadTimeout)
        {
            if (s_downloadBytes != null && downloadTimeout > TimeSpan.Zero)
            {
                long totalMillis = (long)downloadTimeout.TotalMilliseconds;
                CancellationTokenSource? cts = totalMillis > int.MaxValue ? null : new CancellationTokenSource((int)totalMillis);

                try
                {
                    return s_downloadBytes(uri, cts?.Token ?? default);
                }
                catch { }
                finally
                {
                    cts?.Dispose();
                }
            }

            return null;
        }

        [DynamicDependency("#ctor(System.Net.Http.HttpMessageHandler)", "System.Net.Http.HttpClient", "System.Net.Http")]
        [DynamicDependency("#ctor", "System.Net.Http.SocketsHttpHandler", "System.Net.Http")]
        [DynamicDependency("#ctor", "System.Net.Http.HttpRequestMessage", "System.Net.Http")]
        [DynamicDependency("set_PooledConnectionIdleTimeout", "System.Net.Http.SocketsHttpHandler", "System.Net.Http")]
        [DynamicDependency("RequestUri", "System.Net.Http.HttpRequestMessage", "System.Net.Http")]
        [DynamicDependency("Send", "System.Net.Http.HttpClient", "System.Net.Http")]
        [DynamicDependency("Content", "System.Net.Http.HttpResponseMessage", "System.Net.Http")]
        [DynamicDependency("ReadAsStream", "System.Net.Http.HttpContent", "System.Net.Http")]
        private static Func<string, CancellationToken, byte[]>? CreateDownloadBytesFunc()
        {
            try
            {
                // Use reflection to access System.Net.Http:
                // Since System.Net.Http.dll explicitly depends on System.Security.Cryptography.X509Certificates.dll,
                // the latter can't in turn have an explicit dependency on the former.

                // Get the relevant types needed.
                Type? socketsHttpHandlerType = Type.GetType("System.Net.Http.SocketsHttpHandler, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpClientType = Type.GetType("System.Net.Http.HttpClient, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpRequestMessageType = Type.GetType("System.Net.Http.HttpRequestMessage, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpResponseMessageType = Type.GetType("System.Net.Http.HttpResponseMessage, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type? httpContentType = Type.GetType("System.Net.Http.HttpContent, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                if (socketsHttpHandlerType == null || httpClientType == null || httpRequestMessageType == null || httpResponseMessageType == null || httpContentType == null)
                {
                    Debug.Fail("Unable to load required type.");
                    return null;
                }

                // Get the methods on those types.
                PropertyInfo? pooledConnectionIdleTimeoutProp = socketsHttpHandlerType.GetProperty("PooledConnectionIdleTimeout");
                PropertyInfo? requestUriProp = httpRequestMessageType.GetProperty("RequestUri");
                MethodInfo? sendMethod = httpClientType.GetMethod("Send", new Type[] { httpRequestMessageType, typeof(CancellationToken) });
                PropertyInfo? responseContentProp = httpResponseMessageType.GetProperty("Content");
                MethodInfo? readAsStreamMethod = httpContentType.GetMethod("ReadAsStream", Type.EmptyTypes);
                if (pooledConnectionIdleTimeoutProp == null || requestUriProp == null || sendMethod == null || responseContentProp == null || readAsStreamMethod == null)
                {
                    Debug.Fail("Unable to load required member.");
                    return null;
                }

                // Only keep idle connections around briefly, as a compromise between resource leakage and port exhaustion.
                const int PooledConnectionIdleTimeoutSeconds = 15;

                // Equivalent of:
                // var socketsHttpHandler = new SocketsHttpHandler() { PooledConnectionIdleTimeout = TimeSpan.FromSeconds(PooledConnectionIdleTimeoutSeconds) };
                // var httpClient = new HttpClient(socketsHttpHandler);
                object? socketsHttpHandler = Activator.CreateInstance(socketsHttpHandlerType);
                pooledConnectionIdleTimeoutProp.SetValue(socketsHttpHandler, TimeSpan.FromSeconds(PooledConnectionIdleTimeoutSeconds));
                object? httpClient = Activator.CreateInstance(httpClientType, new object?[] { socketsHttpHandler });

                // Return a delegate for getting the byte[] for a uri. This delegate references the HttpClient object and thus
                // all accesses will be through that singleton.
                return (string uri, CancellationToken cancellationToken) =>
                {
                    // Equivalent of:
                    // HttpResponseMessage resp = httpClient.Send(new HttpRequestMessage() { RequestUri = new Uri(uri) });
                    // using Stream responseStream = resp.Content.ReadAsStream();
                    object requestMessage = Activator.CreateInstance(httpRequestMessageType)!;
                    requestUriProp.SetValue(requestMessage, new Uri(uri));
                    object responseMessage = sendMethod.Invoke(httpClient, new object[] { requestMessage, cancellationToken })!;
                    object content = responseContentProp.GetValue(responseMessage)!;
                    using Stream responseStream = (Stream)readAsStreamMethod.Invoke(content, null)!;

                    var result = new MemoryStream();
                    responseStream.CopyTo(result);
                    return result.ToArray();
                };
            }
            catch
            {
                // We shouldn't have any exceptions, but if we do, ignore them all.
                return null;
            }
        }
    }
}
