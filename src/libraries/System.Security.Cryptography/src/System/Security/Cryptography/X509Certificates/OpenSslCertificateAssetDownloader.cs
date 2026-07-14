// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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
                X509ContentType contentType = X509Certificate2.GetCertContentType(data);
                X509Certificate2 certificate;

                switch (contentType)
                {
                    case X509ContentType.Cert:
                        certificate = X509CertificateLoader.LoadCertificate(data);
                        break;
                    case X509ContentType.Pkcs7:
#pragma warning disable SYSLIB0057 // Content is known to be PKCS7.
                        certificate = new X509Certificate2(data);
#pragma warning restore SYSLIB0057
                        break;
                    default:
                        return null;
                }

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

        internal static void ReportCrlCached(string uri)
        {
            RequestCache.Instance.Remove(uri);
        }

        internal static SafeOcspResponseHandle? DownloadOcspGet(string uri, TimeSpan downloadTimeout)
        {
            byte[]? data = DownloadAssetNoCache(uri, downloadTimeout);

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
            return RequestCache.Instance.Get(uri, downloadTimeout);
        }

        private static byte[]? DownloadAssetNoCache(string uri, TimeSpan downloadTimeout)
        {
            return System.Net.Http.X509ResourceClient.DownloadAsset(uri, downloadTimeout);
        }

        private sealed class RequestCache : X509MruCache<CachedRequest>
        {
            private static readonly TimeSpan s_refreshInterval = TimeSpan.FromMinutes(6);

            internal static RequestCache Instance { get; } = new();

            // Each request can be caching a 100MB response, so limit the cache to 10 requests (1GB).
            // The expected size of each entry is more likely in the 10s of kB.
            private RequestCache() : base(10)
            {
            }

            internal byte[]? Get(string uri, TimeSpan downloadTimeout)
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.HttpCacheQuery(uri);
                }

                int hashCode = GetHashCode(uri);
                Node? toRefresh = null;
                CachedRequest? req = null;
                byte[]? ret = null;
                TimeSpan entryAge = default;
                bool ignoredEntry = false;
                Node? evicted = null;
                bool cacheHit = false;

                lock (_lock)
                {
                    if (TryGetNode(hashCode, uri, out Node? cached))
                    {
                        cacheHit = true;
                        req = cached.Value;

                        if (req.DownloadTask.IsCompleted)
                        {
                            byte[]? data = req.DownloadTask.Result;

                            if (data is not null)
                            {
                                if (!req.RefreshInProgress)
                                {
                                    entryAge = DateTimeOffset.UtcNow - req.CacheTime;

                                    if (entryAge > s_refreshInterval)
                                    {
                                        req.RefreshInProgress = true;
                                        toRefresh = cached;
                                    }
                                }

                                ret = data;
                            }
                            else
                            {
                                ignoredEntry = true;
                            }
                        }
                    }

                    if (ignoredEntry || req is null)
                    {
                        // Since the response will be cached, raise the timeout to the default,
                        // if it's lower.
                        TimeSpan timeout = downloadTimeout;

                        if (timeout < ChainPal.DefaultRetrievalTimeout)
                        {
                            timeout = ChainPal.DefaultRetrievalTimeout;
                        }

                        req = AddOrUpdate(
                            hashCode,
                            uri,
                            new CachedRequest(
                                System.Net.Http.X509ResourceClient.DownloadAssetAsync(uri, timeout)),
                            out evicted,
                            replaced: out _);
                    }
                }

                if (toRefresh is not null)
                {
                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.HttpCacheBackgroundRefresh(
                            (int)entryAge.TotalSeconds);
                    }

                    _ = Refresh(toRefresh);
                }

                if (ret is not null)
                {
                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.HttpCacheHitFinished(ret.Length);
                    }

                    return ret;
                }

                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    if (ignoredEntry)
                    {
                        OpenSslX509ChainEventSource.Log.HttpCacheHitIgnored();
                    }
                    else if (cacheHit)
                    {
                        OpenSslX509ChainEventSource.Log.HttpCacheHitInProgress();
                    }
                    else
                    {
                        OpenSslX509ChainEventSource.Log.HttpCacheMiss();

                        if (evicted is not null)
                        {
                            OpenSslX509ChainEventSource.Log.HttpCacheFull(evicted.Key);
                        }
                    }
                }

                Debug.Assert(req is not null);
                Task<byte[]?> downloadTask = req.DownloadTask;

                if (!downloadTask.Wait(downloadTimeout))
                {
                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.HttpCacheTimeout();
                    }

                    return null;
                }

                return downloadTask.Result;
            }

            internal void Remove(string uri)
            {
                Debug.Assert(uri is not null);

                int hashCode = GetHashCode(uri);

                lock (_lock)
                {
                    Remove(hashCode, uri);
                }

                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.HttpCacheExplicitRemoval(uri);
                }
            }

            private async Task Refresh(Node node)
            {
                bool success = false;
                string uri = node.Key;

                try
                {
                    Task<byte[]?> downloadTask = System.Net.Http.X509ResourceClient.DownloadAssetAsync(
                        uri,
                        ChainPal.DefaultRetrievalTimeout);

                    byte[]? resp = await downloadTask.ConfigureAwait(false);

                    if (resp is null)
                    {
                        return;
                    }

                    int hashCode = GetHashCode(uri);
                    CachedRequest updatedResponse = new CachedRequest(downloadTask);

                    lock (_lock)
                    {
                        CachedRequest newValue = AddOrUpdate(
                            hashCode,
                            uri,
                            updatedResponse,
                            evicted: out _,
                            replaced: out _);

                        // If the clock rolls back more than the refresh interval,
                        // AddOrUpdate may decide to keep the pre-refresh value.
                        //
                        // Since temporal physics says that this download is, in fact, "newer",
                        // forcibly remove the old value and add the new one again.
                        //
                        // That's not the same as saying updatedResponse has to be the new value,
                        // it's possible that during the Refresh operation the original value was
                        // removed, and some other value was added in its place.
                        if (ReferenceEquals(newValue, node.Value))
                        {
                            Remove(hashCode, uri);
                            AddOrUpdate(hashCode, uri, updatedResponse, evicted: out _, replaced: out _);
                        }
                    }

                    success = true;

                    if (OpenSslX509ChainEventSource.Log.IsEnabled())
                    {
                        OpenSslX509ChainEventSource.Log.HttpCacheBackgroundSuccess(uri);
                    }
                }
                catch
                {
                    // downloadTask should already be swallowing all exceptions, but just in case,
                    // don't let the opportunistic background refresh end in a faulted state.
                }
                finally
                {
                    if (!success)
                    {
                        if (OpenSslX509ChainEventSource.Log.IsEnabled())
                        {
                            OpenSslX509ChainEventSource.Log.HttpCacheBackgroundFailure(uri);
                        }

                        lock (_lock)
                        {
                            // Strictly speaking, we don't need a lock to set this back to false,
                            // but since this node may still be in the cache, just take the lock
                            // to avoid any mishandling by future code.
                            node.Value.RefreshInProgress = false;
                        }
                    }
                }
            }

            private protected override bool OnConflictTakeNew(Node current, CachedRequest newValue)
            {
                // It shouldn't be the case that we try to overwrite a finished task with a new one,
                // but if it does happen, keep whichever task is done (assuming it finished successfully).

                bool currentUsable = current.Value.DownloadTask.IsCompletedSuccessfully && current.Value.DownloadTask.Result is not null;
                bool newUsable = newValue.DownloadTask.IsCompletedSuccessfully && newValue.DownloadTask.Result is not null;

                if (currentUsable == newUsable)
                {
                    return newValue.CacheTime > current.Value.CacheTime;
                }

                return newUsable;
            }

            private protected override void Pruned(Node? prunedNode, int countStart, int countEnd)
            {
                if (OpenSslX509ChainEventSource.Log.IsEnabled())
                {
                    OpenSslX509ChainEventSource.Log.HttpCachePruned(countStart - countEnd, countEnd);
                }
            }
        }

        private sealed class CachedRequest
        {
            internal bool RefreshInProgress { get; set; }
            internal Task<byte[]?> DownloadTask { get; }
            internal DateTimeOffset CacheTime { get; }

            internal CachedRequest(Task<byte[]?> downloadTask)
            {
                DownloadTask = downloadTask;
                CacheTime = DateTimeOffset.UtcNow;
            }
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
