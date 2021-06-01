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
using HostObject = System.Runtime.InteropServices.JavaScript.HostObject;
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

        public bool AllowAutoRedirect
        {
            get => _allowAutoRedirect;
            set
            {
                _allowAutoRedirect = value;
                _isAllowAutoRedirectTouched = true;
            }
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

        public const bool SupportsAutomaticDecompression = false;
        public const bool SupportsProxy = false;
        public const bool SupportsRedirectConfiguration = true;

        private Dictionary<string, object?>? _properties;
        public IDictionary<string, object?> Properties => _properties ??= new Dictionary<string, object?>();

        protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException ();
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var requestObject = new JSObject();

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
                using (HostObject jsHeaders = new HostObject("Headers"))
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


                WasmHttpReadStream? wasmHttpReadStream = null;

                JSObject abortController = new HostObject("AbortController");
                JSObject signal = (JSObject)abortController.GetObjectProperty("signal");
                requestObject.SetObjectProperty("signal", signal);
                signal.Dispose();

                CancellationTokenSource abortCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                CancellationTokenRegistration abortRegistration = abortCts.Token.Register((Action)(() =>
                {
                    if (abortController.JSHandle != -1)
                    {
                        abortController.Invoke("abort");
                        abortController?.Dispose();
                    }
                    wasmHttpReadStream?.Dispose();
                    abortCts.Dispose();
                }));

                var args = new System.Runtime.InteropServices.JavaScript.Array();
                if (request.RequestUri != null)
                {
                    args.Push(request.RequestUri.ToString());
                    args.Push(requestObject);
                }

                requestObject.Dispose();

                var response = s_fetch?.Invoke("apply", s_window, args) as Task<object>;
                args.Dispose();
                if (response == null)
                    throw new Exception(SR.net_http_marshalling_response_promise_from_fetch);

                JSObject t = (JSObject)await response.ConfigureAwait(continueOnCapturedContext: true);

                var status = new WasmFetchResponse(t, abortController, abortCts, abortRegistration);
                HttpResponseMessage httpResponse = new HttpResponseMessage((HttpStatusCode)status.Status);
                httpResponse.RequestMessage = request;

                // Here we will set the ReasonPhrase so that it can be evaluated later.
                // We do not have a status code but this will signal some type of what happened
                // after interrogating the status code for success or not i.e. IsSuccessStatusCode
                //
                // https://developer.mozilla.org/en-US/docs/Web/API/Response/type
                // opaqueredirect: The fetch request was made with redirect: "manual".
                // The Response's status is 0, headers are empty, body is null and trailer is empty.
                if (status.ResponseType == "opaqueredirect")
                {
                    httpResponse.SetReasonPhraseWithoutValidation(status.ResponseType);
                }

                bool streamingEnabled = false;
                if (StreamingSupported)
                {
                    request.Options.TryGetValue(EnableStreamingResponse, out streamingEnabled);
                }

                httpResponse.Content = streamingEnabled
                    ? new StreamContent(wasmHttpReadStream = new WasmHttpReadStream(status))
                    : (HttpContent)new BrowserHttpContent(status);

                // Fill the response headers
                // CORS will only allow access to certain headers.
                // If a request is made for a resource on another origin which returns the CORs headers, then the type is cors.
                // cors and basic responses are almost identical except that a cors response restricts the headers you can view to
                // `Cache-Control`, `Content-Language`, `Content-Type`, `Expires`, `Last-Modified`, and `Pragma`.
                // View more information https://developers.google.com/web/updates/2015/03/introduction-to-fetch#response_types
                //
                // Note: Some of the headers may not even be valid header types in .NET thus we use TryAddWithoutValidation
                using (JSObject respHeaders = status.Headers)
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
                                        if (!httpResponse.Headers.TryAddWithoutValidation(name, value))
                                            httpResponse.Content.Headers.TryAddWithoutValidation(name, value);
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
                return httpResponse;

            }
            catch (JSException jsExc)
            {
                throw new System.Net.Http.HttpRequestException(jsExc.Message);
            }
        }

        private sealed class WasmFetchResponse : IDisposable
        {
            private readonly JSObject _fetchResponse;
            private readonly JSObject _abortController;
            private readonly CancellationTokenSource _abortCts;
            private readonly CancellationTokenRegistration _abortRegistration;
            private bool _isDisposed;

            public WasmFetchResponse(JSObject fetchResponse, JSObject abortController, CancellationTokenSource abortCts, CancellationTokenRegistration abortRegistration)
            {
                _fetchResponse = fetchResponse ?? throw new ArgumentNullException(nameof(fetchResponse));
                _abortController = abortController ?? throw new ArgumentNullException(nameof(abortController));
                _abortCts = abortCts;
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

                _abortCts.Cancel();
                _abortCts.Dispose();
                _abortRegistration.Dispose();

                _fetchResponse?.Dispose();
                _abortController?.Dispose();
            }
        }

        private sealed class BrowserHttpContent : HttpContent
        {
            private byte[]? _data;
            private readonly WasmFetchResponse _status;

            public BrowserHttpContent(WasmFetchResponse status)
            {
                _status = status ?? throw new ArgumentNullException(nameof(status));
            }

            private async Task<byte[]> GetResponseData()
            {
                if (_data != null)
                {
                    return _data;
                }

                using (System.Runtime.InteropServices.JavaScript.ArrayBuffer dataBuffer = (System.Runtime.InteropServices.JavaScript.ArrayBuffer)await _status.ArrayBuffer().ConfigureAwait(continueOnCapturedContext: true))
                {
                    using (Uint8Array dataBinView = new Uint8Array(dataBuffer))
                    {
                        _data = dataBinView.ToArray();
                        _status.Dispose();
                    }
                }

                return _data;
            }

            protected override async Task<Stream> CreateContentReadStreamAsync()
            {
                byte[] data = await GetResponseData().ConfigureAwait(continueOnCapturedContext: true);
                return new MemoryStream(data, writable: false);
            }

            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context) =>
                SerializeToStreamAsync(stream, context, CancellationToken.None);
            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
            {
                byte[] data = await GetResponseData().ConfigureAwait(continueOnCapturedContext: true);
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
            private WasmFetchResponse? _status;
            private JSObject? _reader;

            private byte[]? _bufferedBytes;
            private int _position;

            public WasmHttpReadStream(WasmFetchResponse status)
            {
                _status = status;
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

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                if (buffer == null)
                {
                    throw new ArgumentNullException(nameof(buffer));
                }
                if (offset < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }
                if (count < 0 || buffer.Length - offset < count)
                {
                    throw new ArgumentOutOfRangeException(nameof(count));
                }

                if (_reader == null)
                {
                    // If we've read everything, then _reader and _status will be null
                    if (_status == null)
                    {
                        return 0;
                    }

                    try
                    {
                        using (JSObject body = _status.Body)
                        {
                            _reader = (JSObject)body.Invoke("getReader");
                        }
                    }
                    catch (JSException)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        throw;
                    }
                }

                if (_bufferedBytes != null && _position < _bufferedBytes.Length)
                {
                    return ReadBuffered();
                }

                try
                {
                    var t = (Task<object>)_reader.Invoke("read");
                    using (var read = (JSObject)await t.ConfigureAwait(continueOnCapturedContext: true))
                    {
                        if ((bool)read.GetObjectProperty("done"))
                        {
                            _reader.Dispose();
                            _reader = null;

                            _status?.Dispose();
                            _status = null;
                            return 0;
                        }

                        _position = 0;
                        // value for fetch streams is a Uint8Array
                        using (Uint8Array binValue = (Uint8Array)read.GetObjectProperty("value"))
                            _bufferedBytes = binValue.ToArray();
                    }
                }
                catch (JSException)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw;
                }

                return ReadBuffered();

                int ReadBuffered()
                {
                    int n = _bufferedBytes.Length - _position;
                    if (n > count)
                        n = count;
                    if (n <= 0)
                        return 0;

                    Buffer.BlockCopy(_bufferedBytes, _position, buffer, offset, n);
                    _position += n;

                    return n;
                }
            }

            protected override void Dispose(bool disposing)
            {
                _reader?.Dispose();
                _status?.Dispose();
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
