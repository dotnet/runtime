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

        private static byte[] DownloadAsset(string uri, ref TimeSpan remainingDownloadTime)
        {
            if (remainingDownloadTime > TimeSpan.Zero)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                object httpClient = null;
                try
                {
                    // Use reflection to access System.Net.Http:
                    // Since System.Net.Http.dll explicitly depends on System.Security.Cryptography.X509Certificates.dll,
                    // the latter can't in turn have an explicit dependency on the former.
                    Type httpClientType = Type.GetType("System.Net.Http.HttpClient, System.Net.Http, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", throwOnError: false);
                    if (httpClientType != null)
                    {
                        MethodInfo getByteArrayAsync = httpClientType.GetMethod("GetByteArrayAsync", new Type[] { typeof(string) });
                        if (getByteArrayAsync != null)
                        {
                            httpClient = Activator.CreateInstance(httpClientType);
                            return ((Task<byte[]>)getByteArrayAsync.Invoke(httpClient, new object[] { uri })).GetAwaiter().GetResult();
                        }
                    }
                }
                catch { }
                finally
                {
                    (httpClient as IDisposable)?.Dispose();

                    // TimeSpan.Zero isn't a worrisome value on the subtraction, it only means "no limit" on the original input.
                    remainingDownloadTime -= stopwatch.Elapsed;
                }
            }

            return null;
        }
    }
}
