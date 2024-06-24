// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        internal static TimeSpan DefaultOcspRefreshInterval => TimeSpan.FromHours(24);
        internal static TimeSpan MinRefreshBeforeExpirationInterval => TimeSpan.FromMinutes(5);
        internal static TimeSpan RefreshAfterFailureBackOffInterval => TimeSpan.FromSeconds(5);

        private const bool TrimRootCertificate = true;
        internal ConcurrentDictionary<SslProtocols, SafeSslContextHandle> SslContexts
        {
            get
            {
                ConcurrentDictionary<SslProtocols, SafeSslContextHandle>? sslContexts = _sslContexts;
                if (sslContexts is null)
                {
                    Interlocked.CompareExchange(ref _sslContexts, new(), null);
                    sslContexts = _sslContexts;
                }

                return sslContexts;
            }
        }

        private ConcurrentDictionary<SslProtocols, SafeSslContextHandle>? _sslContexts;
        internal readonly SafeX509Handle CertificateHandle;
        internal readonly SafeEvpPKeyHandle KeyHandle;

        private bool _staplingForbidden;
        private byte[]? _ocspResponse;
        private DateTimeOffset _ocspExpiration;
        private DateTimeOffset _nextDownload;
        // Private copy of the intermediate certificates, in case the user decides to dispose the
        // instances reachable through IntermediateCertificates property.
        private X509Certificate2[] _privateIntermediateCertificates;
        private X509Certificate2? _rootCertificate;
        private Task<byte[]?>? _pendingDownload;
        private List<string>? _ocspUrls;

        private SslStreamCertificateContext(X509Certificate2 target, ReadOnlyCollection<X509Certificate2> intermediates, SslCertificateTrust? trust)
        {
            IntermediateCertificates = intermediates;
            if (intermediates.Count > 0)
            {
                _privateIntermediateCertificates = new X509Certificate2[intermediates.Count];

                for (int i = 0; i < intermediates.Count; i++)
                {
                    _privateIntermediateCertificates[i] = new X509Certificate2(intermediates[i]);
                }
            }
            else
            {
                _privateIntermediateCertificates = Array.Empty<X509Certificate2>();
            }

            TargetCertificate = target;
            Trust = trust;

            using (RSAOpenSsl? rsa = (RSAOpenSsl?)target.GetRSAPrivateKey())
            {
                if (rsa != null)
                {
                    KeyHandle = rsa.DuplicateKeyHandle();
                }
            }

            if (KeyHandle == null)
            {
                using (ECDsaOpenSsl? ecdsa = (ECDsaOpenSsl?)target.GetECDsaPrivateKey())
                {
                    if (ecdsa != null)
                    {
                        KeyHandle = ecdsa.DuplicateKeyHandle();
                    }
                }

                if (KeyHandle == null)
                {
                    throw new NotSupportedException(SR.net_ssl_io_no_server_cert);
                }
            }

            CertificateHandle = Interop.Crypto.X509UpRef(target.Handle);
        }

        internal static SslStreamCertificateContext Create(X509Certificate2 target) =>
            Create(target, null, offline: false, trust: null, noOcspFetch: true);

        internal bool OcspStaplingAvailable => _ocspUrls is not null;

        partial void SetNoOcspFetch(bool noOcspFetch)
        {
            _staplingForbidden = noOcspFetch;
        }

        partial void AddRootCertificate(X509Certificate2? rootCertificate, ref bool transferredOwnership)
        {
            _rootCertificate = rootCertificate;
            transferredOwnership = rootCertificate != null;

            if (!_staplingForbidden)
            {
                // Create the task, let the download finish in the background.
                GetOcspResponseAsync().AsTask();
            }
        }

        internal byte[]? GetOcspResponseNoWaiting()
        {
            try
            {
                ValueTask<byte[]?> task = GetOcspResponseAsync();

                if (task.IsCompletedSuccessfully)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(this, $"Got OCSP response.");
                    }
                    return task.Result;
                }
            }
            catch
            {
            }

            if (NetEventSource.Log.IsEnabled())
            {
                NetEventSource.Info(this, "No OCSP response available.");
            }
            return null;
        }

        internal ValueTask<byte[]?> GetOcspResponseAsync()
        {
            if (_staplingForbidden)
            {
                return ValueTask.FromResult((byte[]?)null);
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (now > _ocspExpiration)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, "Cached OCSP response expired, fetching fresh staple.");
                }
                return DownloadOcspAsync();
            }

            if (now > _nextDownload)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, "Starting async refresh of OCSP staple");
                }
                // Calling DownloadOcsp will activate a Task to initiate
                // in the background.  Further calls will attach to the
                // same Task if it's still running.
                //
                // We don't want the result here, just the task to background.
#pragma warning disable CA2012 // Use ValueTasks correctly
                DownloadOcspAsync();
#pragma warning restore CA2012 // Use ValueTasks correctly
            }

            return ValueTask.FromResult(_ocspResponse);
        }

        internal ValueTask<byte[]?> WaitForPendingOcspFetchAsync()
        {
            Task<byte[]?>? pending = _pendingDownload;
            if (pending is not null && !pending.IsFaulted)
            {
                return new ValueTask<byte[]?>(pending);
            }

            return ValueTask.FromResult(DateTimeOffset.UtcNow <= _ocspExpiration ? _ocspResponse : null);
        }

        private ValueTask<byte[]?> DownloadOcspAsync()
        {
            Task<byte[]?>? pending = _pendingDownload;

            if (pending is not null && !pending.IsFaulted)
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, $"Pending download task exists.");
                }
                return new ValueTask<byte[]?>(pending);
            }

            if (_ocspUrls is null && _rootCertificate is not null)
            {
                foreach (X509Extension ext in TargetCertificate.Extensions)
                {
                    if (ext is X509AuthorityInformationAccessExtension aia)
                    {
                        foreach (string entry in aia.EnumerateOcspUris())
                        {
                            if (Uri.TryCreate(entry, UriKind.Absolute, out Uri? uri))
                            {
                                if (uri.Scheme == UriScheme.Http)
                                {
                                    (_ocspUrls ??= new List<string>()).Add(entry);
                                }
                            }
                        }

                        break;
                    }
                }
            }

            if (_ocspUrls is null)
            {
                _ocspExpiration = _nextDownload = DateTimeOffset.MaxValue;
                return new ValueTask<byte[]?>((byte[]?)null);
            }

            lock (SslContexts)
            {
                pending = _pendingDownload;

                if (pending is null || pending.IsFaulted)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Info(this, $"Starting new OCSP download task.");
                    }
                    pending = FetchOcspAsync();
                }
            }

            return new ValueTask<byte[]?>(pending);
        }

        private Task<byte[]?> FetchOcspAsync()
        {
            Debug.Assert(_rootCertificate != null);
            X509Certificate2? caCert = _privateIntermediateCertificates.Length > 0 ? _privateIntermediateCertificates[0] : _rootCertificate;

            Debug.Assert(_ocspUrls is not null);
            Debug.Assert(_ocspUrls.Count > 0);
            Debug.Assert(caCert is not null);

            IntPtr subject = TargetCertificate.Handle;
            IntPtr issuer = caCert.Handle;
            Debug.Assert(subject != 0);
            Debug.Assert(issuer != 0);

            // This should not happen - but in the event that it does, we can't give null pointers when building the
            // request, so skip stapling, and set it as forbidden so we don't bother looking for new stapled responses
            // in the future.
            if (subject == 0 || issuer == 0)
            {
                _staplingForbidden = true;
                return Task.FromResult<byte[]?>(null);
            }

            IntPtr[] issuerHandles = ArrayPool<IntPtr>.Shared.Rent(_privateIntermediateCertificates.Length + 1);
            for (int i = 0; i < _privateIntermediateCertificates.Length; i++)
            {
                issuerHandles[i] = _privateIntermediateCertificates[i].Handle;
            }
            issuerHandles[_privateIntermediateCertificates.Length] = _rootCertificate.Handle;

            TaskCompletionSource<byte[]?> completionSource = new TaskCompletionSource<byte[]?>();

            _pendingDownload = completionSource.Task;
            FetchOcspAsyncCore(completionSource);
            return completionSource.Task;

            async void FetchOcspAsyncCore(TaskCompletionSource<byte[]?> completionSource)
            {
                try
                {
                    using SafeOcspRequestHandle ocspRequest = Interop.Crypto.X509BuildOcspRequest(subject, issuer);
                    byte[] rentedBytes = ArrayPool<byte>.Shared.Rent(Interop.Crypto.GetOcspRequestDerSize(ocspRequest));
                    int encodingSize = Interop.Crypto.EncodeOcspRequest(ocspRequest, rentedBytes);
                    ArraySegment<byte> encoded = new ArraySegment<byte>(rentedBytes, 0, encodingSize);

                    ArraySegment<char> rentedChars = UrlBase64Encoding.RentEncode(encoded);
                    byte[]? ret = null;

                    for (int i = 0; i < _ocspUrls.Count; i++)
                    {
                        string url = MakeUrl(_ocspUrls[i], rentedChars);
                        ret = await System.Net.Http.X509ResourceClient.DownloadAssetAsync(url, TimeSpan.MaxValue).ConfigureAwait(false);

                        if (ret is not null)
                        {
                            if (!Interop.Crypto.X509DecodeOcspToExpiration(ret, ocspRequest, subject, issuerHandles.AsSpan(0, _privateIntermediateCertificates.Length + 1), out DateTimeOffset expiration))
                            {
                                ret = null;
                                continue;
                            }

                            // Swap the working URL in as the first one we'll try next time.
                            if (i != 0)
                            {
                                string tmp = _ocspUrls[0];
                                _ocspUrls[0] = _ocspUrls[i];
                                _ocspUrls[i] = tmp;
                            }

                            DateTimeOffset nextCheckA = DateTimeOffset.UtcNow.Add(DefaultOcspRefreshInterval);
                            DateTimeOffset nextCheckB = expiration.Subtract(MinRefreshBeforeExpirationInterval);

                            _ocspResponse = ret;
                            _ocspExpiration = expiration;
                            _nextDownload = nextCheckA < nextCheckB ? nextCheckA : nextCheckB;
                            if (NetEventSource.Log.IsEnabled())
                            {
                                NetEventSource.Info(this, $"Received {ret.Length} B OCSP response, Expiration: {_ocspExpiration}, Next refresh: {_nextDownload}");
                            }
                            break;
                        }
                    }

                    issuerHandles.AsSpan().Clear();
                    ArrayPool<IntPtr>.Shared.Return(issuerHandles);
                    ArrayPool<byte>.Shared.Return(rentedBytes);
                    ArrayPool<char>.Shared.Return(rentedChars.Array!);
                    GC.KeepAlive(TargetCertificate);
                    GC.KeepAlive(_privateIntermediateCertificates);
                    GC.KeepAlive(_rootCertificate);
                    GC.KeepAlive(caCert);

                    if (ret == null)
                    {
                        // All download attempts failed, don't try again for 5 seconds.
                        // This backoff will be applied only if the OCSP staple is not expired.
                        // If it is expired, we will force-refresh it during next GetOcspResponseAsync call.
                        _nextDownload = DateTimeOffset.UtcNow.Add(RefreshAfterFailureBackOffInterval);
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(this, $"OCSP response fetch failed, backing off, Next refresh = {_nextDownload}");
                        }
                    }

                    _pendingDownload = null;
                    completionSource.SetResult(ret);
                }
                catch (Exception ex)
                {
                    if (NetEventSource.Log.IsEnabled())
                    {
                        NetEventSource.Error(this, $"OCSP refresh failed: {ex}");
                    }
                    completionSource.SetException(ex);
                }
            }
        }

        private static string MakeUrl(string baseUri, ArraySegment<char> encodedRequest)
        {
            Debug.Assert(baseUri.Length > 0);
            Debug.Assert(encodedRequest.Count > 0);

            // From https://datatracker.ietf.org/doc/html/rfc6960:
            //
            // An OCSP request using the GET method is constructed as follows:
            //
            //   GET {url}/{url-encoding of base-64 encoding of the DER encoding of
            //   the OCSPRequest}
            //
            // where {url} may be derived from the value of the authority
            // information access extension in the certificate being checked for
            // revocation

            // Since the certificate isn't expected to have a slash at the end, but might,
            // use a custom concat over Uri's built-in combining constructor.

            string uriString;

            if (baseUri.EndsWith('/'))
            {
                uriString = string.Concat(baseUri, encodedRequest.AsSpan());
            }
            else
            {
                uriString = string.Concat(baseUri, "/", encodedRequest.AsSpan());
            }

            return uriString;
        }
    }
}
