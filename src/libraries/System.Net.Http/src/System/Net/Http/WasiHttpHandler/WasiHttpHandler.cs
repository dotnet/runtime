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
        public IncomingResponse? incomingResponse; // owned by this instance
        private readonly OutgoingRequest outgoingRequest; // owned by this instance
        private readonly HttpRequestMessage request;
        private readonly CancellationToken cancellationToken;
        public Task? requestBodyComplete;
        private bool isDisposed;

        public WasiRequestWrapper(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri is null)
            {
                throw new ArgumentException();
            }

            var requestHeaders = WasiHttpInterop.ConvertRequestHeaders(request);
            outgoingRequest = new OutgoingRequest(requestHeaders); // we just passed the Fields ownership to OutgoingRequest
            outgoingRequest.SetMethod(WasiHttpInterop.ConvertMethod(request.Method));
            outgoingRequest.SetScheme(WasiHttpInterop.ConvertScheme(request.RequestUri));
            outgoingRequest.SetAuthority(WasiHttpInterop.ConvertAuthority(request.RequestUri));
            outgoingRequest.SetPathWithQuery(request.RequestUri.PathAndQuery);

            this.request = request;
            this.cancellationToken = cancellationToken;
        }


        public async Task<HttpResponseMessage> SendRequestAsync()
        {
            try
            {
                requestBodyComplete = SendContent();
                incomingResponse = await SendRequest().ConfigureAwait(false);

                ObjectDisposedException.ThrowIf(isDisposed, this);
                cancellationToken.ThrowIfCancellationRequested();

                var response = new HttpResponseMessage((HttpStatusCode)incomingResponse.Status());
                WasiHttpInterop.ConvertResponseHeaders(incomingResponse, response);


                // request body could be still streaming after response headers are received and started streaming response
                // we will leave scope of this method
                // we need to pass the ownership of the request and this wrapper to the response (via response content stream)
                // unless we know that we are not streaming anymore
                WasiInputStream incomingStream = new WasiInputStream(this);// passing self ownership
                response.Content = new StreamContent(incomingStream); // passing incomingStream ownership to SendAsync() caller

                return response;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private async Task<IncomingResponse> SendRequest()
        {
            Console.WriteLine("SendRequestAsync A");
            try
            {
                future = OutgoingHandlerInterop.Handle(outgoingRequest, null);

                while (true)
                {
                    var response = (Result<Result<IncomingResponse, ErrorCode>, None>?)future.Get();
                    if (response.HasValue)
                    {
                        var result = response.Value.AsOk;

                        if (result.IsOk)
                        {
                            Console.WriteLine("SendRequestAsync: response is OK");
                            return result.AsOk;
                        }
                        else
                        {
                            throw new HttpRequestException(WasiHttpInterop.ErrorCodeToString(result.AsErr));
                        }
                    }
                    else
                    {
                        Console.WriteLine("SendRequestAsync B");
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
            catch (WitException e)
            {
                throw new HttpRequestException(WasiHttpInterop.ErrorCodeToString((ErrorCode)e.Value), e);
            }
        }

        public async Task SendContent()
        {
            var content = request.Content;
            if (content is not null)
            {
                wasiOutputStream = new WasiOutputStream(outgoingRequest.Body()); // passing body ownership
                await content.CopyToAsync(wasiOutputStream, cancellationToken).ConfigureAwait(false);
                wasiOutputStream.Close();
            }
        }

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                Console.WriteLine("WasiRequestWrapper.Dispose A");
                wasiOutputStream?.Dispose();
                Console.WriteLine("WasiRequestWrapper.Dispose B");
                incomingResponse?.Dispose();
                Console.WriteLine("WasiRequestWrapper.Dispose C");
                outgoingRequest.Dispose();
                Console.WriteLine("WasiRequestWrapper.Dispose D");
                future?.Dispose();
                Console.WriteLine("WasiRequestWrapper.Dispose E");
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
            var wasiRequest = new WasiRequestWrapper(request, cancellationToken);
            try
            {
                return await wasiRequest.SendRequestAsync().ConfigureAwait(false);
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
