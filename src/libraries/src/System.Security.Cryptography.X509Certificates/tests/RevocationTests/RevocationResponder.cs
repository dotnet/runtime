// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.Asn1;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace System.Security.Cryptography.X509Certificates.Tests.RevocationTests
{
    internal sealed class RevocationResponder : IDisposable
    {
        private static readonly bool s_traceEnabled =
            Environment.GetEnvironmentVariable("TRACE_REVOCATION_RESPONSE") != null;

        private readonly HttpListener _listener;

        private readonly Dictionary<string, CertificateAuthority> _crlPaths
            = new Dictionary<string, CertificateAuthority>();

        private readonly List<(string, CertificateAuthority)> _ocspAuthorities =
            new List<(string, CertificateAuthority)>();

        public string UriPrefix { get; }

        private RevocationResponder(HttpListener listener, string uriPrefix)
        {
            _listener = listener;
            UriPrefix = uriPrefix;
        }

        public void Dispose()
        {
            _listener.Close();
        }

        internal void AddCertificateAuthority(CertificateAuthority authority)
        {
            if (authority.CdpUri != null && authority.CdpUri.StartsWith(UriPrefix))
            {
                _crlPaths.Add(authority.CdpUri.Substring(UriPrefix.Length - 1), authority);
            }

            if (authority.OcspUri != null && authority.OcspUri.StartsWith(UriPrefix))
            {
                _ocspAuthorities.Add((authority.OcspUri.Substring(UriPrefix.Length - 1), authority));
            }
        }

        private void HandleRequests()
        {
            ThreadPool.QueueUserWorkItem(
                state =>
                {
                    while (state._listener.IsListening)
                    {
                        state.HandleRequest();
                    }
                },
                this,
                true);
        }

        internal void HandleRequest()
        {
            HttpListenerContext context = null;

            try
            {
                context = _listener.GetContext();
            }
            catch (Exception)
            {
            }

            if (context != null)
            {
                HandleRequest(context);
            }
        }

        internal async Task HandleRequestAsync()
        {
            HttpListenerContext context = null;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception)
            {
            }

            if (context != null)
            {
                HandleRequest(context);
            }
        }

        internal void HandleRequest(HttpListenerContext context)
        {
            bool responded = false;
            try
            {
                Trace($"{context.Request.HttpMethod} {context.Request.RawUrl} (HTTP {context.Request.ProtocolVersion})");
                HandleRequest(context, ref responded);
            }
            catch (Exception e)
            {
                try
                {
                    if (!responded && context != null)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.StatusDescription = "Internal Server Error";
                        context.Response.Close();

                        Trace($"Sent 500 due to exception on {context.Request.HttpMethod} {context.Request.RawUrl}");
                        Trace(e.ToString());
                    }
                }
                catch (Exception)
                {
                }
                
                return;
            }

            if (!responded)
            {
                Trace($"404 for {context.Request.HttpMethod} {context.Request.RawUrl}");

                try
                {
                    context.Response.StatusCode = 404;
                    context.Response.Close();
                }
                catch (Exception)
                {
                }
            }
        }

        private void HandleRequest(HttpListenerContext context, ref bool responded)
        {
            CertificateAuthority authority;
            string url = context.Request.RawUrl;

            if (_crlPaths.TryGetValue(url, out authority))
            {
                byte[] crl = authority.GetCrl();

                responded = true;
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/pkix-crl";
                context.Response.Close(crl, willBlock: false);
                Trace($"Responded with {crl.Length}-byte CRL from {authority.SubjectName}.");
                return;
            }

            string prefix;

            foreach (var tuple in _ocspAuthorities)
            {
                (prefix, authority) = tuple;

                if (url.StartsWith(prefix))
                {
                    if (context.Request.HttpMethod == "GET")
                    {
                        ReadOnlyMemory<byte> certId;
                        ReadOnlyMemory<byte> nonce;

                        try
                        {
                            string base64 = HttpUtility.UrlDecode(url.Substring(prefix.Length + 1));
                            byte[] reqBytes = Convert.FromBase64String(base64);
                            DecodeOcspRequest(reqBytes, out certId, out nonce);
                        }
                        catch (Exception)
                        {
                            Trace($"OcspRequest Decode failed ({url})");
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                            return;
                        }

                        byte[] ocspResponse = authority.BuildOcspResponse(certId, nonce);

                        responded = true;
                        context.Response.StatusCode = 200;
                        context.Response.StatusDescription = "OK";
                        context.Response.ContentType = "application/ocsp-response";
                        context.Response.Close(ocspResponse, willBlock: false);

                        if (authority.HasOcspDelegation)
                        {
                            Trace($"OCSP Response: {ocspResponse.Length} bytes from {authority.SubjectName} delegated to {authority.OcspResponderSubjectName}");
                        }
                        else
                        {
                            Trace($"OCSP Response: {ocspResponse.Length} bytes from {authority.SubjectName}");
                        }

                        return;
                    }
                }
            }
        }

        internal static RevocationResponder CreateAndListen()
        {
            HttpListener listener = OpenListener(out string uriPrefix);
            
            RevocationResponder responder = new RevocationResponder(listener, uriPrefix);
            responder.HandleRequests();
            return responder;
        }

        private static HttpListener OpenListener(out string uriPrefix)
        {
            while (true)
            {
                int port = RandomNumberGenerator.GetInt32(41000, 42000);
                uriPrefix = $"http://127.0.0.1:{port}/";

                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(uriPrefix);
                listener.IgnoreWriteExceptions = true;

                try
                {
                    listener.Start();
                    Trace($"Listening at {uriPrefix}");
                    return listener;
                }
                catch
                {
                }
            }
        }

        private static void DecodeOcspRequest(
            byte[] requestBytes,
            out ReadOnlyMemory<byte> certId,
            out ReadOnlyMemory<byte> nonceExtension)
        {
            Asn1Tag context0 = new Asn1Tag(TagClass.ContextSpecific, 0);
            Asn1Tag context1 = new Asn1Tag(TagClass.ContextSpecific, 1);

            AsnReader reader = new AsnReader(requestBytes, AsnEncodingRules.DER);
            AsnReader request = reader.ReadSequence();
            reader.ThrowIfNotEmpty();

            AsnReader tbsRequest = request.ReadSequence();

            if (request.HasData)
            {
                // Optional signature
                request.ReadEncodedValue();
                request.ThrowIfNotEmpty();
            }

            // Only v1(0) is supported, and it shouldn't be written per DER.
            // But Apple writes it anyways, so let's go ahead and be lenient.
            if (tbsRequest.PeekTag().HasSameClassAndValue(context0))
            {
                AsnReader versionReader = tbsRequest.ReadSequence(context0);

                if (!versionReader.TryReadInt32(out int version) || version != 0)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                versionReader.ThrowIfNotEmpty();
            }

            if (tbsRequest.PeekTag().HasSameClassAndValue(context1))
            {
                tbsRequest.ReadEncodedValue();
            }

            AsnReader requestList = tbsRequest.ReadSequence();
            AsnReader requestExtensions = null;

            if (tbsRequest.HasData)
            {
                AsnReader requestExtensionsWrapper = tbsRequest.ReadSequence(new Asn1Tag(TagClass.ContextSpecific, 2));
                requestExtensions = requestExtensionsWrapper.ReadSequence();
                requestExtensionsWrapper.ThrowIfNotEmpty();
            }

            tbsRequest.ThrowIfNotEmpty();

            AsnReader firstRequest = requestList.ReadSequence();
            requestList.ThrowIfNotEmpty();

            certId = firstRequest.ReadEncodedValue();

            if (firstRequest.HasData)
            {
                firstRequest.ReadSequence(context0);
            }

            firstRequest.ThrowIfNotEmpty();

            nonceExtension = default;

            if (requestExtensions != null)
            {
                while (requestExtensions.HasData)
                {
                    ReadOnlyMemory<byte> wholeExtension = requestExtensions.PeekEncodedValue();
                    AsnReader extension = requestExtensions.ReadSequence();

                    if (extension.ReadObjectIdentifierAsString() == "1.3.6.1.5.5.7.48.1.2")
                    {
                        nonceExtension = wholeExtension;
                    }
                }
            }
        }

        private static void Trace(string trace)
        {
            if (s_traceEnabled)
            {
                Console.WriteLine(trace);
            }
        }
    }
}
