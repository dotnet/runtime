// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using OpenSslX509ChainEventSource = System.Security.Cryptography.X509Certificates.OpenSslX509ChainEventSource;

namespace System.Security.Cryptography.X509Certificates
{
    internal static class OpenSslCertificateAssetDownloader
    {
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
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.InvalidDownloadedCertificate();
                }

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

            handle.Dispose();

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

                handle.Dispose();
            }

            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.InvalidDownloadedCrl();
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

                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.InvalidDownloadedOcsp();
                }
            }

            return resp;
        }

        private static byte[]? DownloadAsset(string uri, TimeSpan downloadTimeout)
        {
            return System.Net.Http.X509ResourceClient.DownloadAsset(uri, downloadTimeout);
        }
    }
}

namespace System.Net.Http
{
    internal partial class X509ResourceClient
    {
        static partial void ReportNoClient()
        {
            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.HttpClientNotAvailable();
            }
        }

        static partial void ReportNegativeTimeout()
        {
            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.DownloadTimeExceeded();
            }
        }

        static partial void ReportDownloadStart(long totalMillis, string uri)
        {
            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.AssetDownloadStart(totalMillis, uri);
            }
        }

        static partial void ReportDownloadStop(int bytesDownloaded)
        {
            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.AssetDownloadStop(bytesDownloaded);
            }
        }

        static partial void ReportRedirectsExceeded()
        {
            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.DownloadRedirectsExceeded();
            }
        }

        static partial void ReportRedirected(Uri newUri)
        {
            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.DownloadRedirected(newUri);
            }
        }

        static partial void ReportRedirectNotFollowed(Uri redirectUri)
        {
            if (OpenSslX509ChainEventSource.Log.IsEnabled())
            {
                OpenSslX509ChainEventSource.Log.DownloadRedirectNotFollowed(redirectUri);
            }
        }
    }
}
