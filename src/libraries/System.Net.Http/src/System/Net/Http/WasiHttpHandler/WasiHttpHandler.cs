// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WasiHttpWorld;
using WasiHttpWorld.wit.imports.wasi.http.v0_2_1;
using WasiHttpWorld.wit.imports.wasi.io.v0_2_1;
using static WasiHttpWorld.wit.imports.wasi.http.v0_2_1.ITypes;

namespace System.Net.Http
{
    // on top of https://github.com/WebAssembly/wasi-http/blob/main/wit/types.wit
    internal sealed class WasiRequestWrapper : IDisposable
    {
        private FutureIncomingResponse? future; // owned by this instance
        private WasiOutputStream? wasiOutputStream; // owned by this instance
        private WasiInputStream? incomingStream; // owned by this instance

        public Task? requestBodyComplete;
        public Task<IncomingResponse>? requestComplete;
        private bool isDisposed;

        public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is null)
            {
                throw new ArgumentException();
            }

            try
            {
                var requestHeaders = WasiHttpInterop.ConvertRequestHeaders(request);
                var outgoingRequest = new OutgoingRequest(requestHeaders); // passing requestHeaders ownership
                outgoingRequest.SetMethod(WasiHttpInterop.ConvertMethod(request.Method));
                outgoingRequest.SetScheme(WasiHttpInterop.ConvertScheme(request.RequestUri));
                outgoingRequest.SetAuthority(WasiHttpInterop.ConvertAuthority(request.RequestUri));
                outgoingRequest.SetPathWithQuery(request.RequestUri.PathAndQuery);

                requestBodyComplete = SendContent(request.Content, outgoingRequest, cancellationToken);

                future = OutgoingHandlerInterop.Handle(outgoingRequest, null);

                requestComplete = SendRequest(cancellationToken);

                using var incomingResponse = await requestComplete.ConfigureAwait(false);

                ObjectDisposedException.ThrowIf(isDisposed, this);
                cancellationToken.ThrowIfCancellationRequested();

                var response = new HttpResponseMessage((HttpStatusCode)incomingResponse.Status());
                WasiHttpInterop.ConvertResponseHeaders(incomingResponse, response);


                // request body could be still streaming after response headers are received and started streaming response
                // we will leave scope of this method
                // we need to pass the ownership of the request and this wrapper to the response (via response content stream)
                // unless we know that we are not streaming anymore
                incomingStream = new WasiInputStream(this, incomingResponse.Consume());// passing self ownership, passing body ownership
                response.Content = new StreamContent(incomingStream); // passing incomingStream ownership to SendAsync() caller

                return response;
            }
            catch (WitException e)
            {
                Dispose();
                throw new HttpRequestException(WasiHttpInterop.ErrorCodeToString((ErrorCode)e.Value), e);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private async Task<IncomingResponse> SendRequest(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    var response = (Result<Result<IncomingResponse, ErrorCode>, None>?)future!.Get();
                    if (response.HasValue)
                    {
                        var result = response.Value.AsOk;

                        if (result.IsOk)
                        {
                            return result.AsOk;
                        }
                        else
                        {
                            throw new HttpRequestException(WasiHttpInterop.ErrorCodeToString(result.AsErr));
                        }
                    }
                    else
                    {
                        await WasiHttpInterop.RegisterWasiPollable(future.Subscribe(), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException oce)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Http.CancellationHelper.ThrowIfCancellationRequested(oce, cancellationToken);
                }
                throw;
            }
        }

        public async Task SendContent(HttpContent? content, OutgoingRequest outgoingRequest, CancellationToken cancellationToken)
        {
            if (content is not null)
            {
                wasiOutputStream = new WasiOutputStream(outgoingRequest.Body()); // passing body ownership
                await content.CopyToAsync(wasiOutputStream, cancellationToken).ConfigureAwait(false);
                wasiOutputStream.Close();
            }
        }

        ~WasiRequestWrapper()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                wasiOutputStream?.Dispose();
                incomingStream?.Dispose();
                future?.Dispose();
            }
        }
    }

    internal sealed class WasiHttpHandler : HttpMessageHandler
    {
        public const bool SupportsAutomaticDecompression = false;
        public const bool SupportsProxy = false;
        public const bool SupportsRedirectConfiguration = false;

        private Dictionary<string, object?>? _properties;
        public IDictionary<string, object?> Properties => _properties ??= new Dictionary<string, object?>();

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var wasiRequest = new WasiRequestWrapper();
            try
            {
                return await wasiRequest.SendRequestAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // if there was exception or cancellation, we need to dispose the request
                // otherwise it will be disposed by the response
                wasiRequest.Dispose();
                throw;
            }
        }

        #region PlatformNotSupported
#pragma warning disable CA1822

        internal ClientCertificateOption ClientCertificateOptions;


        public bool UseCookies
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public CookieContainer CookieContainer
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public DecompressionMethods AutomaticDecompression
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool UseProxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public IWebProxy? Proxy
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public ICredentials? DefaultProxyCredentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool PreAuthenticate
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public ICredentials? Credentials
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public int MaxAutomaticRedirections
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public int MaxConnectionsPerServer
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public int MaxResponseHeadersLength
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public SslClientAuthenticationOptions SslOptions
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public bool AllowAutoRedirect
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }
#pragma warning restore CA1822

        protected internal override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            throw new PlatformNotSupportedException();
        }

        #endregion
    }
}
