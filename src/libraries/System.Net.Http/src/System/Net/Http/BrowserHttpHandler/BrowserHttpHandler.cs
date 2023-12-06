// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    // **Note** on `Task.ConfigureAwait(continueOnCapturedContext: true)` for the WebAssembly Browser.
    // the JavaScript objects have thread affinity, it is necessary that the continuations run the same thread as the start of the async method.
    internal sealed class BrowserHttpHandler : HttpMessageHandler
    {
        private static readonly HttpRequestOptionsKey<bool> EnableStreamingRequest = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingRequest");
        private static readonly HttpRequestOptionsKey<bool> EnableStreamingResponse = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse");
        private static readonly HttpRequestOptionsKey<IDictionary<string, object>> FetchOptions = new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions");
        private bool _allowAutoRedirect = HttpHandlerDefaults.DefaultAutomaticRedirection;
        // flag to determine if the _allowAutoRedirect was explicitly set or not.
        private bool _isAllowAutoRedirectTouched;

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

        internal ClientCertificateOption ClientCertificateOptions;

        public const bool SupportsAutomaticDecompression = false;
        public const bool SupportsProxy = false;
        public const bool SupportsRedirectConfiguration = true;

#if FEATURE_WASM_THREADS
        private ConcurrentDictionary<string, object?>? _properties;
        public IDictionary<string, object?> Properties => _properties ??= new ConcurrentDictionary<string, object?>();
#else
        private Dictionary<string, object?>? _properties;
        public IDictionary<string, object?> Properties => _properties ??= new Dictionary<string, object?>();
#endif

        protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        private static async Task<WasmFetchResponse> CallFetch(HttpRequestMessage request, CancellationToken cancellationToken, bool? allowAutoRedirect)
        {
            int headerCount = request.Headers.Count + request.Content?.Headers.Count ?? 0;
            List<string> headerNames = new List<string>(headerCount);
            List<string> headerValues = new List<string>(headerCount);
            JSObject abortController = BrowserHttpInterop.CreateAbortController();
            CancellationTokenRegistration abortRegistration = cancellationToken.Register(static s =>
            {
                JSObject _abortController = (JSObject)s!;
#if FEATURE_WASM_THREADS
                if (!_abortController.IsDisposed)
                {
                    _abortController.SynchronizationContext.Send(static (JSObject __abortController) =>
                    {
                        BrowserHttpInterop.AbortRequest(__abortController);
                        __abortController.Dispose();
                    }, _abortController);
                }
#else
                if (!_abortController.IsDisposed)
                {
                    BrowserHttpInterop.AbortRequest(_abortController);
                    _abortController.Dispose();
                }
#endif
            }, abortController);
            try
            {
                if (request.RequestUri == null)
                {
                    throw new ArgumentNullException(nameof(request.RequestUri));
                }

                string uri = request.RequestUri.IsAbsoluteUri ? request.RequestUri.AbsoluteUri : request.RequestUri.ToString();

                bool hasFetchOptions = request.Options.TryGetValue(FetchOptions, out IDictionary<string, object>? fetchOptions);
                int optionCount = 1 + (allowAutoRedirect.HasValue ? 1 : 0) + (hasFetchOptions && fetchOptions != null ? fetchOptions.Count : 0);
                int optionIndex = 0;
                string[] optionNames = new string[optionCount];
                object?[] optionValues = new object?[optionCount];

                optionNames[optionIndex] = "method";
                optionValues[optionIndex] = request.Method.Method;
                optionIndex++;
                if (allowAutoRedirect.HasValue)
                {
                    optionNames[optionIndex] = "redirect";
                    optionValues[optionIndex] = allowAutoRedirect.Value ? "follow" : "manual";
                    optionIndex++;
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

                if (hasFetchOptions && fetchOptions != null)
                {
                    foreach (KeyValuePair<string, object> item in fetchOptions)
                    {
                        optionNames[optionIndex] = item.Key;
                        optionValues[optionIndex] = item.Value;
                        optionIndex++;
                    }
                }

                JSObject? fetchResponse;
                cancellationToken.ThrowIfCancellationRequested();
                if (request.Content != null)
                {
                    bool streamingEnabled = false;
                    if (BrowserHttpInterop.SupportsStreamingRequest())
                    {
                        request.Options.TryGetValue(EnableStreamingRequest, out streamingEnabled);
                    }

                    if (streamingEnabled)
                    {
                        using (JSObject transformStream = BrowserHttpInterop.CreateTransformStream())
                        {
                            Task<JSObject> fetchPromise = BrowserHttpInterop.Fetch(uri, headerNames.ToArray(), headerValues.ToArray(), optionNames, optionValues, abortController, transformStream);
                            Task<JSObject> fetchTask = BrowserHttpInterop.CancelationHelper(fetchPromise, cancellationToken).AsTask(); // initialize fetch cancellation

                            using (WasmHttpWriteStream stream = new WasmHttpWriteStream(transformStream))
                            {
                                try
                                {
                                    await request.Content.CopyToAsync(stream, cancellationToken).ConfigureAwait(true);
                                    Task closePromise = BrowserHttpInterop.TransformStreamClose(transformStream);
                                    await BrowserHttpInterop.CancelationHelper(closePromise, cancellationToken).ConfigureAwait(true);
                                }
                                catch (Exception)
                                {
                                    BrowserHttpInterop.TransformStreamAbort(transformStream);
                                    if (!abortController.IsDisposed)
                                    {
                                        BrowserHttpInterop.AbortRequest(abortController);
                                    }
                                    try
                                    {
                                        using (fetchResponse = await fetchTask.ConfigureAwait(true)) // observe exception
                                        {
                                            BrowserHttpInterop.AbortResponse(fetchResponse);
                                        }
                                    }
                                    catch { /* ignore */ }
                                    cancellationToken.ThrowIfCancellationRequested();
                                    throw;
                                }
                            }

                            fetchResponse = await fetchTask.ConfigureAwait(true);
                        }
                    }
                    else
                    {
                        byte[] buffer = await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(true);
                        cancellationToken.ThrowIfCancellationRequested();

                        Task<JSObject> fetchPromise = BrowserHttpInterop.Fetch(uri, headerNames.ToArray(), headerValues.ToArray(), optionNames, optionValues, abortController, buffer);
                        fetchResponse = await BrowserHttpInterop.CancelationHelper(fetchPromise, cancellationToken).ConfigureAwait(true);
                    }
                }
                else
                {
                    Task<JSObject> fetchPromise = BrowserHttpInterop.Fetch(uri, headerNames.ToArray(), headerValues.ToArray(), optionNames, optionValues, abortController);
                    fetchResponse = await BrowserHttpInterop.CancelationHelper(fetchPromise, cancellationToken).ConfigureAwait(true);
                }

                return new WasmFetchResponse(fetchResponse, abortController, abortRegistration);
            }
            catch (Exception ex)
            {
                abortRegistration.Dispose();
                abortController.Dispose();
                if (ex is JSException jse)
                {
                    throw new HttpRequestException(jse.Message, jse);
                }
                throw;
            }
        }

        private static HttpResponseMessage ConvertResponse(HttpRequestMessage request, WasmFetchResponse fetchResponse)
        {
#if FEATURE_WASM_THREADS
            lock (fetchResponse.ThisLock)
            {
#endif
                fetchResponse.ThrowIfDisposed();
                string? responseType = fetchResponse.FetchResponse!.GetPropertyAsString("type")!;
                int status = fetchResponse.FetchResponse.GetPropertyAsInt32("status");
                HttpResponseMessage responseMessage = new HttpResponseMessage((HttpStatusCode)status);
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
                    responseMessage.SetReasonPhraseWithoutValidation(responseType);
                }

                bool streamingEnabled = false;
                if (BrowserHttpInterop.SupportsStreamingResponse())
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
#if FEATURE_WASM_THREADS
            } //lock
#endif
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool? allowAutoRedirect = _isAllowAutoRedirectTouched ? AllowAutoRedirect : null;
#if FEATURE_WASM_THREADS
            return JSHost.CurrentOrMainJSSynchronizationContext.Post(() =>
            {
#endif
                return Impl(request, cancellationToken, allowAutoRedirect);
#if FEATURE_WASM_THREADS
            });
#endif

            static async Task<HttpResponseMessage> Impl(HttpRequestMessage request, CancellationToken cancellationToken, bool? allowAutoRedirect)
            {
                WasmFetchResponse fetchRespose = await CallFetch(request, cancellationToken, allowAutoRedirect).ConfigureAwait(true);
                return ConvertResponse(request, fetchRespose);
            }
        }
    }

    internal sealed class WasmHttpWriteStream : Stream
    {
        private readonly JSObject _transformStream;

        public WasmHttpWriteStream(JSObject transformStream)
        {
            ArgumentNullException.ThrowIfNull(transformStream);

            _transformStream = transformStream;
        }

        private Task WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
#if FEATURE_WASM_THREADS
            return _transformStream.SynchronizationContext.Post(() => Impl(this, buffer, cancellationToken));
#else
            return Impl(this, buffer, cancellationToken);
#endif
            static async Task Impl(WasmHttpWriteStream self, ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
            {
                using (Buffers.MemoryHandle handle = buffer.Pin())
                {
                    Task writePromise = TransformStreamWriteUnsafe(self._transformStream, buffer, handle);
                    await BrowserHttpInterop.CancelationHelper(writePromise, cancellationToken).ConfigureAwait(true);
                }
            }

            static unsafe Task TransformStreamWriteUnsafe(JSObject transformStream, ReadOnlyMemory<byte> buffer, Buffers.MemoryHandle handle)
                => BrowserHttpInterop.TransformStreamWrite(transformStream, (nint)handle.Pointer, buffer.Length);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            return new ValueTask(WriteAsyncCore(buffer, cancellationToken));
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return WriteAsyncCore(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken);
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        protected override void Dispose(bool disposing)
        {
            _transformStream.Dispose();
        }

        public override void Flush()
        {
        }

        #region PlatformNotSupported

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Length => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            throw new NotSupportedException(SR.net_http_synchronous_writes_not_supported);
        }
        #endregion
    }

    internal sealed class WasmFetchResponse : IDisposable
    {
#if FEATURE_WASM_THREADS
        public readonly object ThisLock = new object();
#endif
        public JSObject? FetchResponse;
        private readonly JSObject _abortController;
        private readonly CancellationTokenRegistration _abortRegistration;
        private bool _isDisposed;

        public WasmFetchResponse(JSObject fetchResponse, JSObject abortController, CancellationTokenRegistration abortRegistration)
        {
            ArgumentNullException.ThrowIfNull(fetchResponse);
            ArgumentNullException.ThrowIfNull(abortController);

            FetchResponse = fetchResponse;
            _abortRegistration = abortRegistration;
            _abortController = abortController;
        }

        public void ThrowIfDisposed()
        {
#if FEATURE_WASM_THREADS
            lock (ThisLock)
            {
#endif
                ObjectDisposedException.ThrowIf(_isDisposed, this);
#if FEATURE_WASM_THREADS
            } //lock
#endif
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

#if FEATURE_WASM_THREADS
            FetchResponse?.SynchronizationContext.Post(static (WasmFetchResponse self) =>
            {
                lock (self.ThisLock)
                {
                    if (!self._isDisposed)
                    {
                        self._isDisposed = true;
                        self._abortRegistration.Dispose();
                        self._abortController.Dispose();
                        if (!self.FetchResponse!.IsDisposed)
                        {
                            BrowserHttpInterop.AbortResponse(self.FetchResponse);
                        }
                        self.FetchResponse.Dispose();
                        self.FetchResponse = null;
                    }
                    return Task.CompletedTask;
                }
            }, this);
#else
            _isDisposed = true;
            _abortRegistration.Dispose();
            _abortController.Dispose();
            if (FetchResponse != null)
            {
                if (!FetchResponse.IsDisposed)
                {
                    BrowserHttpInterop.AbortResponse(FetchResponse);
                }
                FetchResponse.Dispose();
                FetchResponse = null;
            }
#endif
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

        // TODO allocate smaller buffer and call multiple times
        private async ValueTask<byte[]> GetResponseData(CancellationToken cancellationToken)
        {
            Task<int> promise;
#if FEATURE_WASM_THREADS
            lock (_fetchResponse.ThisLock)
            {
#endif
                if (_data != null)
                {
                    return _data;
                }
                _fetchResponse.ThrowIfDisposed();
                promise = BrowserHttpInterop.GetResponseLength(_fetchResponse.FetchResponse!);
#if FEATURE_WASM_THREADS
            } //lock
#endif
            _length = await BrowserHttpInterop.CancelationHelper(promise, cancellationToken, _fetchResponse.FetchResponse).ConfigureAwait(true);
#if FEATURE_WASM_THREADS
            lock (_fetchResponse.ThisLock)
            {
#endif
                _data = new byte[_length];

                BrowserHttpInterop.GetResponseBytes(_fetchResponse.FetchResponse!, new Span<byte>(_data));

                return _data;
#if FEATURE_WASM_THREADS
            } //lock
#endif
        }

        protected override Task<Stream> CreateContentReadStreamAsync()
        {
            _fetchResponse.ThrowIfDisposed();
#if FEATURE_WASM_THREADS
            return _fetchResponse.FetchResponse!.SynchronizationContext.Post(() => Impl(this));
#else
            return Impl(this);
#endif
            static async Task<Stream> Impl(BrowserHttpContent self)
            {
                self._fetchResponse.ThrowIfDisposed();
                byte[] data = await self.GetResponseData(CancellationToken.None).ConfigureAwait(true);
                return new MemoryStream(data, writable: false);
            }
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));
            _fetchResponse.ThrowIfDisposed();
#if FEATURE_WASM_THREADS
            return _fetchResponse.FetchResponse!.SynchronizationContext.Post(() => Impl(this, stream, cancellationToken));
#else
            return Impl(this, stream, cancellationToken);
#endif

            static async Task Impl(BrowserHttpContent self, Stream stream, CancellationToken cancellationToken)
            {
                self._fetchResponse.ThrowIfDisposed();
                byte[] data = await self.GetResponseData(cancellationToken).ConfigureAwait(true);
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(true);
            }
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
            _fetchResponse.Dispose();
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
#if FEATURE_WASM_THREADS
            return await _fetchResponse.FetchResponse!.SynchronizationContext.Post(() => Impl(this, buffer, cancellationToken)).ConfigureAwait(true);
#else
            return await Impl(this, buffer, cancellationToken).ConfigureAwait(true);
#endif

            static async Task<int> Impl(WasmHttpReadStream self, Memory<byte> buffer, CancellationToken cancellationToken)
            {
                self._fetchResponse.ThrowIfDisposed();
                Task<int> promise;
                using (Buffers.MemoryHandle handle = buffer.Pin())
                {
#if FEATURE_WASM_THREADS
                    lock (self._fetchResponse.ThisLock)
                    {
#endif
                        self._fetchResponse.ThrowIfDisposed();
                        promise = GetStreamedResponseBytesUnsafe(self._fetchResponse, buffer, handle);
#if FEATURE_WASM_THREADS
                    } //lock
#endif
                    int response = await BrowserHttpInterop.CancelationHelper(promise, cancellationToken, self._fetchResponse.FetchResponse).ConfigureAwait(true);
                    return response;
                }

                unsafe static Task<int> GetStreamedResponseBytesUnsafe(WasmFetchResponse _fetchResponse, Memory<byte> buffer, Buffers.MemoryHandle handle)
                    => BrowserHttpInterop.GetStreamedResponseBytes(_fetchResponse.FetchResponse!, (IntPtr)handle.Pointer, buffer.Length);
            }
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            ValidateBufferArguments(buffer, offset, count);
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;

        protected override void Dispose(bool disposing)
        {
            _fetchResponse.Dispose();
        }

        public override void Flush()
        {
        }

        #region PlatformNotSupported

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Length => throw new NotSupportedException();
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
        #endregion
    }
}
