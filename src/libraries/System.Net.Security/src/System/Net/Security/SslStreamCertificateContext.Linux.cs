// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Security
{
    public partial class SslStreamCertificateContext
    {
        private const bool TrimRootCertificate = true;
        internal readonly ConcurrentDictionary<SslProtocols, SafeSslContextHandle> SslContexts;

        private bool _staplingForbidden;
        private byte[]? _ocspResponse;
        private DateTimeOffset _ocspExpiration;
        private DateTimeOffset _nextDownload;
        private Task<byte[]?>? _pendingDownload;
        private List<string>? _ocspUrls;
        private X509Certificate2? _ca;

        private SslStreamCertificateContext(X509Certificate2 target, X509Certificate2[] intermediates, SslCertificateTrust? trust)
        {
            Certificate = target;
            IntermediateCertificates = intermediates;
            Trust = trust;
            SslContexts = new ConcurrentDictionary<SslProtocols, SafeSslContextHandle>();
        }

        internal static SslStreamCertificateContext Create(X509Certificate2 target) =>
            Create(target, null, offline: false, trust: null, noOcspFetch: true);

        internal bool OcspStaplingAvailable => _ocspUrls is not null;

        partial void SetNoOcspFetch(bool noOcspFetch)
        {
            _staplingForbidden = noOcspFetch;
        }

        partial void AddRootCertificate(X509Certificate2? rootCertificate)
        {
            if (IntermediateCertificates.Length == 0)
            {
                _ca = rootCertificate;
            }
            else
            {
                _ca = IntermediateCertificates[0];
            }

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
                    return task.Result;
                }
            }
            catch
            {
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
                return DownloadOcspAsync();
            }

            if (now > _nextDownload)
            {
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

        private ValueTask<byte[]?> DownloadOcspAsync()
        {
            Task<byte[]?>? pending = _pendingDownload;

            if (pending is not null && !pending.IsFaulted)
            {
                return new ValueTask<byte[]?>(pending);
            }

            if (_ocspUrls is null && _ca is not null)
            {
                foreach (X509Extension ext in Certificate.Extensions)
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
                    _pendingDownload = pending = FetchOcspAsync();
                }
            }

            return new ValueTask<byte[]?>(pending);
        }

        private async Task<byte[]?> FetchOcspAsync()
        {
            X509Certificate2? caCert = _ca;
            Debug.Assert(_ocspUrls is not null);
            Debug.Assert(_ocspUrls.Count > 0);
            Debug.Assert(caCert is not null);

            IntPtr subject = Certificate.Handle;
            IntPtr issuer = caCert.Handle;

            using (SafeOcspRequestHandle ocspRequest = Interop.Crypto.X509BuildOcspRequest(subject, issuer))
            {
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
                        if (!Interop.Crypto.X509DecodeOcspToExpiration(ret, ocspRequest, subject, issuer, out DateTimeOffset expiration))
                        {
                            continue;
                        }

                        // Swap the working URL in as the first one we'll try next time.
                        if (i != 0)
                        {
                            string tmp = _ocspUrls[0];
                            _ocspUrls[0] = _ocspUrls[i];
                            _ocspUrls[i] = tmp;
                        }

                        DateTimeOffset nextCheckA = DateTimeOffset.UtcNow.AddDays(1);
                        DateTimeOffset nextCheckB = expiration.AddMinutes(-5);

                        _ocspResponse = ret;
                        _ocspExpiration = expiration;
                        _nextDownload = nextCheckA < nextCheckB ? nextCheckA : nextCheckB;
                        _pendingDownload = null;
                        break;
                    }
                }

                ArrayPool<byte>.Shared.Return(rentedBytes);
                ArrayPool<char>.Shared.Return(rentedChars.Array!);
                GC.KeepAlive(Certificate);
                GC.KeepAlive(caCert);
                return ret;
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
