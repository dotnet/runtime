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
using WasiHttpWorld.wit.Imports.wasi.http.v0_2_8;
using WasiHttpWorld.wit.Imports.wasi.io.v0_2_8;
using static WasiHttpWorld.wit.Imports.wasi.http.v0_2_8.ITypesImports;

namespace System.Net.Http
{
    // on top of https://github.com/WebAssembly/wasi-http/blob/main/wit/types.wit
    internal sealed class WasiRequestWrapper : IDisposable
    {
        private WasiOutputStream? wasiOutputStream; // disposal could be made earlier from this instance's Dispose
        private WasiInputStream? incomingStream; // disposal could be made earlier from this instance's Dispose
        private FutureIncomingResponse? future; // owned by this instance
        private bool isDisposed;

        public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is not { IsAbsoluteUri: true } requestUri)
            {
                throw new InvalidOperationException(SR.net_http_client_invalid_requesturi);
            }

            if (!string.Equals(requestUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && !string.Equals(requestUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotSupportedException(SR.Format(SR.net_http_unsupported_requesturi_scheme, requestUri.Scheme));
            }

            try
            {
                var requestHeaders = WasiHttpInterop.ConvertRequestHeaders(request);
                var outgoingRequest = new OutgoingRequest(requestHeaders); // passing requestHeaders ownership
                outgoingRequest.SetMethod(WasiHttpInterop.ConvertMethod(request.Method));
                outgoingRequest.SetScheme(WasiHttpInterop.ConvertScheme(request.RequestUri));
                outgoingRequest.SetAuthority(WasiHttpInterop.ConvertAuthority(request.RequestUri));
                outgoingRequest.SetPathWithQuery(request.RequestUri.PathAndQuery);

                // The request body is streamed concurrently with waiting for the response,
                // which may arrive before the body has been fully sent.
#pragma warning disable CA2025
                Task sendContentTask = SendContent(request.Content, outgoingRequest, cancellationToken);
#pragma warning restore CA2025

                future = IOutgoingHandlerImports.Handle(outgoingRequest, null);

                using var incomingResponse = await SendRequest(sendContentTask, cancellationToken).ConfigureAwait(false);

                // Observe failures that happen after the response has started arriving.
                _ = sendContentTask.ContinueWith(static t => _ = t.Exception, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

                ObjectDisposedException.ThrowIf(isDisposed, this);
                cancellationToken.ThrowIfCancellationRequested();

                var response = new HttpResponseMessage((HttpStatusCode)incomingResponse.Status());
                response.RequestMessage = request;

                // request body could be still streaming after response headers are received and started streaming response
                // we will leave scope of this method
                // we need to pass the ownership of the request and this wrapper to the response (via response content stream)
                // unless we know that we are not streaming anymore
                incomingStream = new WasiInputStream(this, incomingResponse.Consume());// passing self ownership, passing body ownership
                response.Content = new StreamContent(incomingStream); // passing incomingStream ownership to SendAsync() caller
                WasiHttpInterop.ConvertResponseHeaders(incomingResponse, response);

                return response;
            }
            catch (WitException e)
            {
                throw new HttpRequestException(WasiHttpInterop.ErrorCodeToString((ErrorCode)e.Value), e);
            }
            catch (Exception)
            {
                throw;
            }
        }

        private async Task<IncomingResponse> SendRequest(Task sendContentTask, CancellationToken cancellationToken)
        {
            using var pollCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
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
                        Task pollTask = WasiHttpInterop.RegisterWasiPollable(future.Subscribe(), pollCts.Token);
                        if (!sendContentTask.IsCompleted)
                        {
                            await Task.WhenAny(pollTask, sendContentTask).ConfigureAwait(false);
                        }

                        if (!sendContentTask.IsCompletedSuccessfully && sendContentTask.IsCompleted)
                        {
                            // Sending the request content failed, so no response will arrive; fail the request with the content's exception.
                            pollCts.Cancel();
                            await pollTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                            await sendContentTask.ConfigureAwait(false);
                        }

                        await pollTask.ConfigureAwait(false);
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
                try
                {
                    await content.CopyToAsync(wasiOutputStream, cancellationToken).ConfigureAwait(false);
                }
                catch (WitException)
                {
                    // The host stopped accepting the request body, for example because the server already responded.
                    // The outcome of the request is reported through the response future.
                    return;
                }
                wasiOutputStream.Finish();
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

        public IDictionary<string, object?> Properties => field ??= new Dictionary<string, object?>();

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
