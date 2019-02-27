// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal static class CertificateAssetDownloader
    {
        private static readonly Func<string, byte[]> s_downloadBytes = CreateDownloadBytesFunc();

        internal static X509Certificate2 DownloadCertificate(string uri, ref TimeSpan remainingDownloadTime)
        {
            byte[] data = DownloadAsset(uri, ref remainingDownloadTime);

            if (data == null)
            {
                return null;
            }

            try
            {
                return new X509Certificate2(data);
            }
            catch (CryptographicException)
            {
                return null;
            }
        }

        internal static SafeX509CrlHandle DownloadCrl(string uri, ref TimeSpan remainingDownloadTime)
        {
            byte[] data = DownloadAsset(uri, ref remainingDownloadTime);

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

        internal static SafeOcspResponseHandle DownloadOcspGet(string uri, ref TimeSpan remainingDownloadTime)
        {
            byte[] data = DownloadAsset(uri, ref remainingDownloadTime);

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

        private static byte[] DownloadAsset(string uri, ref TimeSpan remainingDownloadTime)
        {
            if (s_downloadBytes != null && remainingDownloadTime > TimeSpan.Zero)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                try
                {
                    return s_downloadBytes(uri);
                }
                catch { }
                finally
                {
                    // TimeSpan.Zero isn't a worrisome value on the subtraction, it only means "no limit" on the original input.
                    remainingDownloadTime -= stopwatch.Elapsed;
                }
            }

            return null;
        }

        private static Func<string, byte[]> CreateDownloadBytesFunc()
        {
            try
            {
                // Use reflection to access System.Net.Http:
                // Since System.Net.Http.dll explicitly depends on System.Security.Cryptography.X509Certificates.dll,
                // the latter can't in turn have an explicit dependency on the former.

                Type socketsHttpHandlerType = Type.GetType("System.Net.Http.SocketsHttpHandler, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                Type httpClientType = Type.GetType("System.Net.Http.HttpClient, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                if (socketsHttpHandlerType == null || httpClientType == null)
                {
                    return null;
                }

                PropertyInfo pooledConnectionIdleTimeoutProp = socketsHttpHandlerType.GetProperty("PooledConnectionIdleTimeout");
                MethodInfo getByteArrayAsyncMethod = httpClientType.GetMethod("GetByteArrayAsync", new Type[] { typeof(string) });
                if (pooledConnectionIdleTimeoutProp == null || getByteArrayAsyncMethod == null)
                {
                    return null;
                }

                // Only keep idle connections around briefly, as a compromise between resource leakage and port exhaustion.
                const int PooledConnectionIdleTimeoutSeconds = 15;

                // Equivalent of:
                // var socketsHttpHandler = new SocketsHttpHandler();
                // var httpClient = new HttpClient(socketsHttpHandler);
                // socketsHttpHandler.PooledConnectionIdleTimeout = TimeSpan.FromSeconds(PooledConnectionIdleTimeoutSeconds);
                // Func<string, Task<byte[]>> getByteArrayAsync = httpClient.GetByteArrayAsync;
                object socketsHttpHandler = Activator.CreateInstance(socketsHttpHandlerType);
                object httpClient = Activator.CreateInstance(httpClientType, new object[] { socketsHttpHandler });
                pooledConnectionIdleTimeoutProp.SetValue(socketsHttpHandler, TimeSpan.FromSeconds(PooledConnectionIdleTimeoutSeconds));
                var getByteArrayAsync = (Func<string, Task<byte[]>>)getByteArrayAsyncMethod.CreateDelegate(
                    typeof(Func<string, Task<byte[]>>),
                    httpClient);

                // Return a delegate for getting the byte[] for a uri.
                // This delegate references the HttpClient object and thus
                // all accesses will be through that singleton.
                return uri => getByteArrayAsync(uri).GetAwaiter().GetResult();
            }
            catch
            {
                // We shouldn't have any exceptions, but if we do, ignore them all.
                return null;
            }
        }
    }
}
