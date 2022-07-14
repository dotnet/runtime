// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices.JavaScript;

namespace System.Net.Http
{
    // **Note** on `Task.ConfigureAwait(continueOnCapturedContext: true)` for the WebAssembly Browser.
    // The current implementation of WebAssembly for the Browser does not have a SynchronizationContext nor a Scheduler
    // thus forcing the callbacks to run on the main browser thread.  When threading is eventually implemented using
    // emscripten's threading model of remote worker threads, via SharedArrayBuffer, any API calls will have to be
    // remoted back to the main thread.  Most APIs only work on the main browser thread.
    // During discussions the concensus has been that it will not matter right now which value is used for ConfigureAwait
    // we should put this in place now.
    internal sealed class BrowserHttpHandler : HttpMessageHandler
    {
        private static readonly HttpRequestOptionsKey<bool> EnableStreamingResponse = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse");
        private static readonly HttpRequestOptionsKey<IDictionary<string, object>> FetchOptions = new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions");
        private bool _allowAutoRedirect = HttpHandlerDefaults.DefaultAutomaticRedirection;
        // flag to determine if the _allowAutoRedirect was explicitly set or not.
        private bool _isAllowAutoRedirectTouched;

        /// <summary>
        /// Gets whether the current Browser supports streaming responses
        /// </summary>
        private static bool StreamingSupported { get; } = BrowserHttpInterop.SupportsStreamingResponse();

        #region PlatformNotSupported
#pragma warning disable CA1822
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
#pragma warning restore CA1822
        #endregion

        public bool AllowAutoRedirect
        {
            get => _allowAutoRedirect;
            set
            {
                _allowAutoRedirect = value;
                _isAllowAutoRedirectTouched = true;
            }
        }

        public const bool SupportsAutomaticDecompression = false;
        public const bool SupportsProxy = false;
        public const bool SupportsRedirectConfiguration = true;

        private Dictionary<string, object?>? _properties;
        public IDictionary<string, object?> Properties => _properties ??= new Dictionary<string, object?>();

        protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        private static async Task<WasmFetchResponse> CallFetch(HttpRequestMessage request, CancellationToken cancellationToken, bool? allowAutoRedirect)
        {
            int headerCount = request.Headers.Count + request.Content?.Headers.Count ?? 0;
            List<string> headerNames = new List<string>(headerCount);
            List<string> headerValues = new List<string>(headerCount);
            List<string> optionNames = new List<string>();
            List<object?> optionValues = new List<object?>();

            JSObject abortController = BrowserHttpInterop.CreateAbortController();
            CancellationTokenRegistration? abortRegistration = cancellationToken.Register(() =>
            {
                if (!abortController.IsDisposed)
                {
                    BrowserHttpInterop.AbortRequest(abortController);
                }
                abortController.Dispose();
            });
            try
            {
                optionNames.Add("method");
                optionValues.Add(request.Method.Method);
                if (allowAutoRedirect.HasValue)
                {
                    optionNames.Add("redirect");
                    optionValues.Add(allowAutoRedirect.Value ? "follow" : "manual");
                }

                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
                {
                    foreach (string value in header.Value)
                    {
                        headerNames.Add(header.Key);
                        headerValues.Add(value);
                    }
                }

                if (request.Content != null)
                {
                    foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                    {
                        foreach (string value in header.Value)
                        {
                            headerNames.Add(header.Key);
                            headerValues.Add(value);
                        }
                    }
                }

                if (request.Options.TryGetValue(FetchOptions, out IDictionary<string, object>? fetchOptions))
                {
                    foreach (KeyValuePair<string, object> item in fetchOptions)
                    {
                        optionNames.Add(item.Key);
                        optionValues.Add(item.Value);
                    }
                }

                if (request.RequestUri == null)
                {
                    throw new ArgumentNullException(nameof(request.RequestUri));
                }

                string uri = request.RequestUri.IsAbsoluteUri ? request.RequestUri.AbsoluteUri : request.RequestUri.ToString();
                Task<JSObject>? promise;
                cancellationToken.ThrowIfCancellationRequested();
                if (request.Content != null)
                {
                    if (request.Content is StringContent)
                    {
                        string body = await request.Content.ReadAsStringAsync(cancellationToken)
                            .ConfigureAwait(true);
                        cancellationToken.ThrowIfCancellationRequested();

                        promise = BrowserHttpInterop.Fetch(uri, headerNames.ToArray(), headerValues.ToArray(), optionNames.ToArray(), optionValues.ToArray(), abortController, body);
                    }
                    else
                    {
                        byte[] buffer = await request.Content.ReadAsByteArrayAsync(cancellationToken)
                            .ConfigureAwait(true);
                        cancellationToken.ThrowIfCancellationRequested();

                        promise = BrowserHttpInterop.Fetch(uri, headerNames.ToArray(), headerValues.ToArray(), optionNames.ToArray(), optionValues.ToArray(), abortController, buffer);
                    }
                }
                else
                {
                    promise = BrowserHttpInterop.Fetch(uri, headerNames.ToArray(), headerValues.ToArray(), optionNames.ToArray(), optionValues.ToArray(), abortController);
                }
                cancellationToken.ThrowIfCancellationRequested();
                ValueTask<JSObject> wrappedTask = BrowserHttpInterop.CancelationHelper(promise, cancellationToken, abortController);
                JSObject fetchResponse = await wrappedTask.ConfigureAwait(true);
                return new WasmFetchResponse(fetchResponse, abortRegistration.Value);
            }
            catch (Exception)
            {
                // this would also trigger abort
                abortRegistration?.Dispose();
                throw;
            }
        }

        private static HttpResponseMessage ConvertResponse(HttpRequestMessage request, WasmFetchResponse fetchResponse)
        {
            string? responseType = fetchResponse.ResponseType;
            HttpResponseMessage responseMessage = new HttpResponseMessage((HttpStatusCode)fetchResponse.Status);
            responseMessage.RequestMessage = request;
            if (responseType == "opaqueredirect")
            {
                // Here we will set the ReasonPhrase so that it can be evaluated later.
                // We do not have a status code but this will signal some type of what happened
                // after interrogating the status code for success or not i.e. IsSuccessStatusCode
                //
                // https://developer.mozilla.org/en-US/docs/Web/API/Response/type
                // opaqueredirect: The fetch request was made with redirect: "manual".
                // The Response's status is 0, headers are empty, body is null and trailer is empty.
                responseMessage.SetReasonPhraseWithoutValidation(fetchResponse.ResponseType);
            }

            bool streamingEnabled = false;
            if (StreamingSupported)
            {
                request.Options.TryGetValue(EnableStreamingResponse, out streamingEnabled);
            }

            responseMessage.Content = streamingEnabled
                ? new StreamContent(new WasmHttpReadStream(fetchResponse))
                : new BrowserHttpContent(fetchResponse);


            // Some of the headers may not even be valid header types in .NET thus we use TryAddWithoutValidation
            // CORS will only allow access to certain headers on browser.
            BrowserHttpInterop.GetResponseHeaders(fetchResponse.FetchResponse, responseMessage.Headers, responseMessage.Content.Headers);

            return responseMessage;
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Impl(request, cancellationToken, _isAllowAutoRedirectTouched ? AllowAutoRedirect : null);

            static async Task<HttpResponseMessage> Impl(HttpRequestMessage request, CancellationToken cancellationToken, bool? allowAutoRedirect)
            {
                WasmFetchResponse fetchRespose = await CallFetch(request, cancellationToken, allowAutoRedirect).ConfigureAwait(true);
                return ConvertResponse(request, fetchRespose);
            }
        }
    }

    internal sealed class WasmFetchResponse : IDisposable
    {
        public readonly JSObject FetchResponse;
        private readonly CancellationTokenRegistration _abortRegistration;
        private bool _isDisposed;

        public WasmFetchResponse(JSObject fetchResponse, CancellationTokenRegistration abortRegistration)
        {
            ArgumentNullException.ThrowIfNull(fetchResponse);

            FetchResponse = fetchResponse;
            _abortRegistration = abortRegistration;
        }

        public string ResponseType
        {
            get
            {
                return FetchResponse.GetPropertyAsString("type")!;
            }
        }

        public int Status
        {
            get
            {
                return FetchResponse.GetPropertyAsInt32("status");
            }
        }

        public void ThrowIfDisposed()
        {
            if (_isDisposed && FetchResponse.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(WasmFetchResponse));
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _abortRegistration.Dispose();

            if (FetchResponse != null && !FetchResponse.IsDisposed)
            {
                BrowserHttpInterop.AbortResponse(FetchResponse);
            }
            FetchResponse?.Dispose();
        }
    }

    internal sealed class BrowserHttpContent : HttpContent
    {
        private byte[]? _data;
        private int _length = -1;
        private readonly WasmFetchResponse _fetchResponse;

        public BrowserHttpContent(WasmFetchResponse fetchResponse)
        {
            ArgumentNullException.ThrowIfNull(fetchResponse);

            _fetchResponse = fetchResponse;
        }

        // TODO alocate smaller buffer and call multiple times
        private async ValueTask<byte[]> GetResponseData(CancellationToken cancellationToken)
        {
            if (_data != null)
            {
                return _data;
            }
            _fetchResponse.ThrowIfDisposed();
            Task<int> promise = BrowserHttpInterop.GetResponseLength(_fetchResponse.FetchResponse);
            _length = await BrowserHttpInterop.CancelationHelper(promise, cancellationToken, null, _fetchResponse.FetchResponse).ConfigureAwait(true);
            _data = new byte[_length];

            BrowserHttpInterop.GetResponseBytes(_fetchResponse.FetchResponse, new Span<byte>(_data));

            return _data;
        }

        protected override async Task<Stream> CreateContentReadStreamAsync()
        {
            byte[] data = await GetResponseData(CancellationToken.None).ConfigureAwait(true);
            return new MemoryStream(data, writable: false);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));
            byte[] data = await GetResponseData(cancellationToken).ConfigureAwait(true);
            await stream.WriteAsync(data, cancellationToken).ConfigureAwait(true);
        }

        protected internal override bool TryComputeLength(out long length)
        {
            if (_length != -1)
            {
                length = _length;
                return true;
            }

            length = 0;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            _fetchResponse?.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class WasmHttpReadStream : Stream
    {
        private WasmFetchResponse _fetchResponse;

        public WasmHttpReadStream(WasmFetchResponse fetchResponse)
        {
            _fetchResponse = fetchResponse;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));

            _fetchResponse.ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            using (Buffers.MemoryHandle handle = buffer.Pin())
            {
                Task<int> promise = GetStreamedResponseBytesUnsafe(_fetchResponse, buffer, handle);
                int response = await BrowserHttpInterop.CancelationHelper(promise, cancellationToken, null, _fetchResponse.FetchResponse).ConfigureAwait(true);
                return response;
            }

            unsafe static Task<int> GetStreamedResponseBytesUnsafe(WasmFetchResponse _fetchResponse, Memory<byte> buffer, System.Buffers.MemoryHandle handle)
                => BrowserHttpInterop.GetStreamedResponseBytes(_fetchResponse.FetchResponse, (IntPtr)handle.Pointer, buffer.Length);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            _fetchResponse?.Dispose();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException(SR.net_http_synchronous_reads_not_supported);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
