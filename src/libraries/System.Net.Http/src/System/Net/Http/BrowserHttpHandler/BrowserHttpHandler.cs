// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using JSObject = System.Runtime.InteropServices.JavaScript.JSObject;
using JSException = System.Runtime.InteropServices.JavaScript.JSException;
using Uint8Array = System.Runtime.InteropServices.JavaScript.Uint8Array;
using Function = System.Runtime.InteropServices.JavaScript.Function;

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
        // This partial implementation contains members common to Browser WebAssembly running on .NET Core.
        private static readonly JSObject? s_fetch = (JSObject)System.Runtime.InteropServices.JavaScript.Runtime.GetGlobalObject("fetch");
        private static readonly JSObject? s_window = (JSObject)System.Runtime.InteropServices.JavaScript.Runtime.GetGlobalObject("window");

        private static readonly HttpRequestOptionsKey<bool> EnableStreamingResponse = new HttpRequestOptionsKey<bool>("WebAssemblyEnableStreamingResponse");
        private static readonly HttpRequestOptionsKey<IDictionary<string, object>> FetchOptions = new HttpRequestOptionsKey<IDictionary<string, object>>("WebAssemblyFetchOptions");
        private bool _allowAutoRedirect = HttpHandlerDefaults.DefaultAutomaticRedirection;
        // flag to determine if the _allowAutoRedirect was explicitly set or not.
        private bool _isAllowAutoRedirectTouched;

        /// <summary>
        /// Gets whether the current Browser supports streaming responses
        /// </summary>
        private static bool StreamingSupported { get; } = GetIsStreamingSupported();
        private static bool GetIsStreamingSupported()
        {
            using (var streamingSupported = new Function("return typeof Response !== 'undefined' && 'body' in Response.prototype && typeof ReadableStream === 'function'"))
                return (bool)streamingSupported.Call();
        }

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

        protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);
            return Impl(request, cancellationToken);

            async Task<HttpResponseMessage> Impl(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                CancellationTokenRegistration? abortRegistration = null;
                try
                {
                    using var requestObject = new JSObject();

                    if (request.Options.TryGetValue(FetchOptions, out IDictionary<string, object>? fetchOptions))
                    {
                        foreach (KeyValuePair<string, object> item in fetchOptions)
                        {
                            requestObject.SetObjectProperty(item.Key, item.Value);
                        }
                    }

                    requestObject.SetObjectProperty("method", request.Method.Method);

                    // Only set if property was specifically modified and is not default value
                    if (_isAllowAutoRedirectTouched)
                    {
                        // Allowing or Disallowing redirects.
                        // Here we will set redirect to `manual` instead of error if AllowAutoRedirect is
                        // false so there is no exception thrown
                        //
                        // https://developer.mozilla.org/en-US/docs/Web/API/Response/type
                        //
                        // other issues from whatwg/fetch:
                        //
                        // https://github.com/whatwg/fetch/issues/763
                        // https://github.com/whatwg/fetch/issues/601
                        requestObject.SetObjectProperty("redirect", AllowAutoRedirect ? "follow" : "manual");
                    }

                    // We need to check for body content
                    if (request.Content != null)
                    {
                        if (request.Content is StringContent)
                        {
                            requestObject.SetObjectProperty("body", await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: true));
                        }
                        else
                        {
                            using (Uint8Array uint8Buffer = Uint8Array.From(await request.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: true)))
                            {
                                requestObject.SetObjectProperty("body", uint8Buffer);
                            }
                        }
                    }

                    // Process headers
                    // Cors has its own restrictions on headers.
                    // https://developer.mozilla.org/en-US/docs/Web/API/Headers
                    using (JSObject jsHeaders = new JSObject("Headers"))
                    {
                        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
                        {
                            foreach (string value in header.Value)
                            {
                                jsHeaders.Invoke("append", header.Key, value);
                            }
                        }
                        if (request.Content != null)
                        {
                            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers)
                            {
                                foreach (string value in header.Value)
                                {
                                    jsHeaders.Invoke("append", header.Key, value);
                                }
                            }
                        }
                        requestObject.SetObjectProperty("headers", jsHeaders);
                    }


                    JSObject abortController = new JSObject("AbortController");
                    using JSObject signal = (JSObject)abortController.GetObjectProperty("signal");
                    requestObject.SetObjectProperty("signal", signal);

                    abortRegistration = cancellationToken.Register(() =>
                    {
                        if (!abortController.IsDisposed)
                        {
                            abortController.Invoke("abort");
                            abortController?.Dispose();
                        }
                    });

                    using var args = new System.Runtime.InteropServices.JavaScript.Array();
                    if (request.RequestUri != null)
                    {
                        args.Push(request.RequestUri.IsAbsoluteUri ? request.RequestUri.AbsoluteUri : request.RequestUri.ToString());
                        args.Push(requestObject);
                    }


                    var responseTask = s_fetch?.Invoke("apply", s_window, args) as Task<object>;
                    if (responseTask == null)
                        throw new Exception(SR.net_http_marshalling_response_promise_from_fetch);

                    cancellationToken.ThrowIfCancellationRequested();

                    var fetchResponseJs = (JSObject)await responseTask.ConfigureAwait(continueOnCapturedContext: true);

                    var fetchResponse = new WasmFetchResponse(fetchResponseJs, abortController, abortRegistration.Value);
                    abortRegistration = null;
                    var responseMessage = new HttpResponseMessage((HttpStatusCode)fetchResponse.Status);
                    responseMessage.RequestMessage = request;

                    // Here we will set the ReasonPhrase so that it can be evaluated later.
                    // We do not have a status code but this will signal some type of what happened
                    // after interrogating the status code for success or not i.e. IsSuccessStatusCode
                    //
                    // https://developer.mozilla.org/en-US/docs/Web/API/Response/type
                    // opaqueredirect: The fetch request was made with redirect: "manual".
                    // The Response's status is 0, headers are empty, body is null and trailer is empty.
                    if (fetchResponse.ResponseType == "opaqueredirect")
                    {
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

                    // Fill the response headers
                    // CORS will only allow access to certain headers.
                    // If a request is made for a resource on another origin which returns the CORs headers, then the type is cors.
                    // cors and basic responses are almost identical except that a cors response restricts the headers you can view to
                    // `Cache-Control`, `Content-Language`, `Content-Type`, `Expires`, `Last-Modified`, and `Pragma`.
                    // View more information https://developers.google.com/web/updates/2015/03/introduction-to-fetch#response_types
                    //
                    // Note: Some of the headers may not even be valid header types in .NET thus we use TryAddWithoutValidation
                    using (JSObject respHeaders = fetchResponse.Headers)
                    {
                        if (respHeaders != null)
                        {
                            using (var entriesIterator = (JSObject)respHeaders.Invoke("entries"))
                            {
                                JSObject? nextResult = null;
                                try
                                {
                                    nextResult = (JSObject)entriesIterator.Invoke("next");
                                    while (!(bool)nextResult.GetObjectProperty("done"))
                                    {
                                        using (var resultValue = (System.Runtime.InteropServices.JavaScript.Array)nextResult.GetObjectProperty("value"))
                                        {
                                            var name = (string)resultValue[0];
                                            var value = (string)resultValue[1];
                                            if (!responseMessage.Headers.TryAddWithoutValidation(name, value))
                                                responseMessage.Content.Headers.TryAddWithoutValidation(name, value);
                                        }
                                        nextResult?.Dispose();
                                        nextResult = (JSObject)entriesIterator.Invoke("next");
                                    }
                                }
                                finally
                                {
                                    nextResult?.Dispose();
                                }
                            }
                        }
                    }
                    return responseMessage;

                }
                catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
                {
                    throw CancellationHelper.CreateOperationCanceledException(oce, cancellationToken);
                }
                catch (JSException jse)
                {
                    throw TranslateJSException(jse, cancellationToken);
                }
                finally
                {
                    abortRegistration?.Dispose();
                }
            }
        }

        private static Exception TranslateJSException(JSException jse, CancellationToken cancellationToken)
        {
            if (jse.Message.StartsWith("AbortError", StringComparison.Ordinal))
            {
                return CancellationHelper.CreateOperationCanceledException(jse, CancellationToken.None);
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return CancellationHelper.CreateOperationCanceledException(jse, cancellationToken);
            }
            return new HttpRequestException(jse.Message, jse);
        }

        private sealed class WasmFetchResponse : IDisposable
        {
            private readonly JSObject _fetchResponse;
            private readonly JSObject _abortController;
            private readonly CancellationTokenRegistration _abortRegistration;
            private bool _isDisposed;

            public WasmFetchResponse(JSObject fetchResponse, JSObject abortController, CancellationTokenRegistration abortRegistration)
            {
                ArgumentNullException.ThrowIfNull(fetchResponse);
                ArgumentNullException.ThrowIfNull(abortController);

                _fetchResponse = fetchResponse;
                _abortController = abortController;
                _abortRegistration = abortRegistration;
            }

            public bool IsOK => (bool)_fetchResponse.GetObjectProperty("ok");
            public bool IsRedirected => (bool)_fetchResponse.GetObjectProperty("redirected");
            public int Status => (int)_fetchResponse.GetObjectProperty("status");
            public string StatusText => (string)_fetchResponse.GetObjectProperty("statusText");
            public string ResponseType => (string)_fetchResponse.GetObjectProperty("type");
            public string Url => (string)_fetchResponse.GetObjectProperty("url");
            public bool IsBodyUsed => (bool)_fetchResponse.GetObjectProperty("bodyUsed");
            public JSObject Headers => (JSObject)_fetchResponse.GetObjectProperty("headers");
            public JSObject Body => (JSObject)_fetchResponse.GetObjectProperty("body");

            public Task<object> ArrayBuffer() => (Task<object>)_fetchResponse.Invoke("arrayBuffer");
            public Task<object> Text() => (Task<object>)_fetchResponse.Invoke("text");
            public Task<object> JSON() => (Task<object>)_fetchResponse.Invoke("json");

            public void Dispose()
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                _abortRegistration.Dispose();

                _fetchResponse?.Dispose();
                if (_abortController != null && !_abortController.IsDisposed)
                {
                    _abortController.Invoke("abort");
                }
                _abortController?.Dispose();
            }
        }

        private sealed class BrowserHttpContent : HttpContent
        {
            private byte[]? _data;
            private readonly WasmFetchResponse _status;

            public BrowserHttpContent(WasmFetchResponse status)
            {
                ArgumentNullException.ThrowIfNull(status);

                _status = status;
            }

            private async Task<byte[]> GetResponseData(CancellationToken cancellationToken)
            {
                if (_data != null)
                {
                    return _data;
                }
                try
                {
                    using (System.Runtime.InteropServices.JavaScript.ArrayBuffer dataBuffer = (System.Runtime.InteropServices.JavaScript.ArrayBuffer)await _status.ArrayBuffer().ConfigureAwait(continueOnCapturedContext: true))
                    {
                        using (Uint8Array dataBinView = new Uint8Array(dataBuffer))
                        {
                            _data = dataBinView.ToArray();
                            _status.Dispose();
                        }
                    }
                }
                catch (JSException jse)
                {
                    throw TranslateJSException(jse, cancellationToken);
                }

                return _data;
            }

            protected override async Task<Stream> CreateContentReadStreamAsync()
            {
                byte[] data = await GetResponseData(CancellationToken.None).ConfigureAwait(continueOnCapturedContext: true);
                return new MemoryStream(data, writable: false);
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
                SerializeToStreamAsync(stream, context, CancellationToken.None);
            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            {
                byte[] data = await GetResponseData(cancellationToken).ConfigureAwait(continueOnCapturedContext: true);
                await stream.WriteAsync(data, cancellationToken).ConfigureAwait(continueOnCapturedContext: true);
            }
            protected internal override bool TryComputeLength(out long length)
            {
                if (_data != null)
                {
                    length = _data.Length;
                    return true;
                }

                length = 0;
                return false;
            }

            protected override void Dispose(bool disposing)
            {
                _status?.Dispose();
                base.Dispose(disposing);
            }
        }

        private sealed class WasmHttpReadStream : Stream
        {
            private WasmFetchResponse? _fetchResponse;
            private JSObject? _reader;

            private byte[]? _bufferedBytes;
            private int _position;

            public WasmHttpReadStream(WasmFetchResponse fetchResponse)
            {
                _fetchResponse = fetchResponse;
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

            public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                ValidateBufferArguments(buffer, offset, count);
                return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
            }

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
            {
                CancellationHelper.ThrowIfCancellationRequested(cancellationToken);

                if (_reader == null)
                {
                    // If we've read everything, then _reader and _status will be null
                    if (_fetchResponse == null)
                    {
                        return 0;
                    }

                    try
                    {
                        using (JSObject body = _fetchResponse.Body)
                        {
                            _reader = (JSObject)body.Invoke("getReader");
                        }
                    }
                    catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
                    {
                        throw CancellationHelper.CreateOperationCanceledException(oce, cancellationToken);
                    }
                    catch (JSException jse)
                    {
                        throw TranslateJSException(jse, cancellationToken);
                    }
                }

                using var abortRegistration = cancellationToken.Register(() =>
                {
                    _reader.Invoke("cancel");
                });

                if (_bufferedBytes != null && _position < _bufferedBytes.Length)
                {
                    return ReadBuffered();
                }

                try
                {
                    var t = (Task<object>)_reader.Invoke("read");
                    using (var read = (JSObject)await t.ConfigureAwait(continueOnCapturedContext: true))
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            _reader.Invoke("cancel");
                            throw CancellationHelper.CreateOperationCanceledException(null, cancellationToken);
                        }

                        if ((bool)read.GetObjectProperty("done"))
                        {
                            _reader.Dispose();
                            _reader = null;

                            _fetchResponse?.Dispose();
                            _fetchResponse = null;
                            return 0;
                        }

                        _position = 0;
                        // value for fetch streams is a Uint8Array
                        using (Uint8Array binValue = (Uint8Array)read.GetObjectProperty("value"))
                            _bufferedBytes = binValue.ToArray();
                    }
                }
                catch (OperationCanceledException oce) when (cancellationToken.IsCancellationRequested)
                {
                    throw CancellationHelper.CreateOperationCanceledException(oce, cancellationToken);
                }
                catch (JSException jse)
                {
                    throw TranslateJSException(jse, cancellationToken);
                }

                return ReadBuffered();

                int ReadBuffered()
                {
                    int n = Math.Min(_bufferedBytes.Length - _position, buffer.Length);
                    if (n <= 0)
                    {
                        return 0;
                    }

                    _bufferedBytes.AsSpan(_position, n).CopyTo(buffer.Span);
                    _position += n;

                    return n;
                }
            }

            protected override void Dispose(bool disposing)
            {
                _reader?.Dispose();
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
}
