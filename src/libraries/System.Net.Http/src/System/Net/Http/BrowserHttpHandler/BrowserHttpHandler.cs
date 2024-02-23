// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
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

        private Dictionary<string, object?>? _properties;
        public IDictionary<string, object?> Properties => _properties ??= new Dictionary<string, object?>();

        protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            bool? allowAutoRedirect = _isAllowAutoRedirectTouched ? AllowAutoRedirect : null;
            var controller = new BrowserHttpController(request, allowAutoRedirect, cancellationToken);
            return controller.CallFetch();
        }
    }

    internal sealed class BrowserHttpController : IDisposable
    {
        private static readonly HttpRequestOptionsKey<bool> EnableStreamingRequest = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingRequest");
        private static readonly HttpRequestOptionsKey<bool> EnableStreamingResponse = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse");
        private static readonly HttpRequestOptionsKey<IDictionary<string, object>> FetchOptions = new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions");

        internal readonly JSObject _jsController;
        private readonly CancellationTokenRegistration _abortRegistration;
        private readonly string[] _optionNames;
        private readonly object?[] _optionValues;
        private readonly string[] _headerNames;
        private readonly string[] _headerValues;
        private readonly string uri;
        private readonly CancellationToken _cancellationToken;
        private readonly HttpRequestMessage _request;
        private bool _isDisposed;

        public BrowserHttpController(HttpRequestMessage request, bool? allowAutoRedirect, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            if (request.RequestUri == null)
            {
                throw new ArgumentNullException(nameof(request.RequestUri));
            }

            _cancellationToken = cancellationToken;
            _request = request;

            JSObject httpController = BrowserHttpInterop.CreateController();
            CancellationTokenRegistration abortRegistration = cancellationToken.Register(static s =>
            {
                JSObject _httpController = (JSObject)s!;

                if (!_httpController.IsDisposed)
                {
                    BrowserHttpInterop.AbortRequest(_httpController);
                }
            }, httpController);


            _jsController = httpController;
            _abortRegistration = abortRegistration;

            uri = request.RequestUri.IsAbsoluteUri ? request.RequestUri.AbsoluteUri : request.RequestUri.ToString();

            bool hasFetchOptions = request.Options.TryGetValue(FetchOptions, out IDictionary<string, object>? fetchOptions);
            int optionCount = 1 + (allowAutoRedirect.HasValue ? 1 : 0) + (hasFetchOptions && fetchOptions != null ? fetchOptions.Count : 0);
            int optionIndex = 0;

            // note there could be more values for each header name and so this is just name count
            int headerCount = request.Headers.Count + (request.Content?.Headers.Count ?? 0);

            _optionNames = new string[optionCount];
            _optionValues = new object?[optionCount];

            _optionNames[optionIndex] = "method";
            _optionValues[optionIndex] = request.Method.Method;
            optionIndex++;
            if (allowAutoRedirect.HasValue)
            {
                _optionNames[optionIndex] = "redirect";
                _optionValues[optionIndex] = allowAutoRedirect.Value ? "follow" : "manual";
                optionIndex++;
            }

            if (hasFetchOptions && fetchOptions != null)
            {
                foreach (KeyValuePair<string, object> item in fetchOptions)
                {
                    _optionNames[optionIndex] = item.Key;
                    _optionValues[optionIndex] = item.Value;
                    optionIndex++;
                }
            }

            var headerNames = new List<string>(headerCount);
            var headerValues = new List<string>(headerCount);

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
            _headerNames = headerNames.ToArray();
            _headerValues = headerValues.ToArray();
        }

        public async Task<HttpResponseMessage> CallFetch()
        {
            CancellationHelper.ThrowIfCancellationRequested(_cancellationToken);

            BrowserHttpWriteStream? writeStream = null;
            Task fetchPromise;
            bool streamingRequestEnabled = false;

            try
            {
                if (_request.Content != null)
                {
                    if (BrowserHttpInterop.SupportsStreamingRequest())
                    {
                        _request.Options.TryGetValue(EnableStreamingRequest, out streamingRequestEnabled);
                    }

                    if (streamingRequestEnabled)
                    {
                        fetchPromise = BrowserHttpInterop.FetchStream(_jsController, uri, _headerNames, _headerValues, _optionNames, _optionValues);
                        writeStream = new BrowserHttpWriteStream(this);
                        await _request.Content.CopyToAsync(writeStream, _cancellationToken).ConfigureAwait(false);
                        var closePromise = BrowserHttpInterop.TransformStreamClose(_jsController);
                        await BrowserHttpInterop.CancellationHelper(closePromise, _cancellationToken, _jsController).ConfigureAwait(false);
                    }
                    else
                    {
                        byte[] buffer = await _request.Content.ReadAsByteArrayAsync(_cancellationToken).ConfigureAwait(false);
                        CancellationHelper.ThrowIfCancellationRequested(_cancellationToken);

                        Memory<byte> bufferMemory = buffer.AsMemory();
                        // http_wasm_fetch_byte makes a copy of the bytes synchronously, so we can un-pin it synchronously
                        using MemoryHandle pinBuffer = bufferMemory.Pin();
                        fetchPromise = BrowserHttpInterop.FetchBytes(_jsController, uri, _headerNames, _headerValues, _optionNames, _optionValues, pinBuffer, buffer.Length);
                    }
                }
                else
                {
                    fetchPromise = BrowserHttpInterop.Fetch(_jsController, uri, _headerNames, _headerValues, _optionNames, _optionValues);
                }
                await BrowserHttpInterop.CancellationHelper(fetchPromise, _cancellationToken, _jsController).ConfigureAwait(false);

                return ConvertResponse();
            }
            catch (Exception ex)
            {
                Dispose(); // will also abort request
                if (ex is JSException jse)
                {
                    throw new HttpRequestException(jse.Message, jse);
                }
                throw;
            }
            finally
            {
                writeStream?.Dispose();
            }
        }

        private HttpResponseMessage ConvertResponse()
        {
            lock (this)
            {
                ThrowIfDisposed();
                string? responseType = BrowserHttpInterop.GetResponseType(_jsController);
                int status = BrowserHttpInterop.GetResponseStatus(_jsController);
                HttpResponseMessage responseMessage = new HttpResponseMessage((HttpStatusCode)status);
                responseMessage.RequestMessage = _request;
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

                bool streamingResponseEnabled = false;
                if (BrowserHttpInterop.SupportsStreamingResponse())
                {
                    _request.Options.TryGetValue(EnableStreamingResponse, out streamingResponseEnabled);
                }

                responseMessage.Content = streamingResponseEnabled
                    ? new StreamContent(new BrowserHttpReadStream(this))
                    : new BrowserHttpContent(this);

                BrowserHttpInterop.GetResponseHeaders(_jsController!, responseMessage.Headers, responseMessage.Content.Headers);

                return responseMessage;
            } //lock
        }

        public void ThrowIfDisposed()
        {
            lock (this)
            {
                ObjectDisposedException.ThrowIf(_isDisposed, this);
            } //lock
        }

        public void Dispose()
        {
            lock (this)
            {
                if (_isDisposed)
                    return;
                _isDisposed = true;
            }
            _abortRegistration.Dispose();
            if (_jsController != null)
            {
                if (!_jsController.IsDisposed)
                {
                    BrowserHttpInterop.AbortRequest(_jsController);// aborts also response
                }
                _jsController.Dispose();
            }
        }
    }

    internal sealed class BrowserHttpWriteStream : Stream
    {
        private readonly BrowserHttpController _controller; // we don't own it, we don't dispose it from here
        public BrowserHttpWriteStream(BrowserHttpController controller)
        {
            ArgumentNullException.ThrowIfNull(controller);

            _controller = controller;
        }

        private Task WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            CancellationHelper.ThrowIfCancellationRequested(cancellationToken);
            _controller.ThrowIfDisposed();

            // http_wasm_transform_stream_write makes a copy of the bytes synchronously, so we can dispose the handle synchronously
            using MemoryHandle pinBuffer = buffer.Pin();
            Task writePromise = BrowserHttpInterop.TransformStreamWriteUnsafe(_controller._jsController, buffer, pinBuffer);
            return BrowserHttpInterop.CancellationHelper(writePromise, cancellationToken, _controller._jsController);
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

    internal sealed class BrowserHttpContent : HttpContent
    {
        private byte[]? _data;
        private int _length = -1;
        private readonly BrowserHttpController _controller;

        public BrowserHttpContent(BrowserHttpController controller)
        {
            ArgumentNullException.ThrowIfNull(controller);
            _controller = controller;
        }

        // TODO allocate smaller buffer and call multiple times
        private async ValueTask<byte[]> GetResponseData(CancellationToken cancellationToken)
        {
            Task<int> promise;
            lock (_controller)
            {
                if (_data != null)
                {
                    return _data;
                }
                _controller.ThrowIfDisposed();
                promise = BrowserHttpInterop.GetResponseLength(_controller._jsController!);
            } //lock
            _length = await BrowserHttpInterop.CancellationHelper(promise, cancellationToken, _controller._jsController).ConfigureAwait(false);
            lock (_controller)
            {
                _data = new byte[_length];

                BrowserHttpInterop.GetResponseBytes(_controller._jsController!, new Span<byte>(_data));

                return _data;
            } //lock
        }

        protected override async Task<Stream> CreateContentReadStreamAsync()
        {
            byte[] data = await GetResponseData(CancellationToken.None).ConfigureAwait(false);
            return new MemoryStream(data, writable: false);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
            SerializeToStreamAsync(stream, context, CancellationToken.None);

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(stream, nameof(stream));

            byte[] data = await GetResponseData(cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
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
            _controller.Dispose();
            base.Dispose(disposing);
        }
    }

    internal sealed class BrowserHttpReadStream : Stream
    {
        private BrowserHttpController _controller; // we own the object and have to dispose it

        public BrowserHttpReadStream(BrowserHttpController controller)
        {
            _controller = controller;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(buffer, nameof(buffer));
            _controller.ThrowIfDisposed();

            MemoryHandle pinBuffer = buffer.Pin();
            int bytesCount;
            try
            {
                _controller.ThrowIfDisposed();

                var promise = BrowserHttpInterop.GetStreamedResponseBytesUnsafe(_controller._jsController, buffer, pinBuffer);
                bytesCount = await BrowserHttpInterop.CancellationHelper(promise, cancellationToken, _controller._jsController).ConfigureAwait(false);
            }
            finally
            {
                // this must be after await, because http_wasm_get_streamed_response_bytes is using the buffer in a continuation
                pinBuffer.Dispose();
            }
            return bytesCount;
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
            _controller.Dispose();
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
