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
using WasiHttpWorld.wit.imports.wasi.http.v0_2_0;
using WasiHttpWorld.wit.imports.wasi.io.v0_2_0;

namespace System.Net.Http
{
    internal sealed class WasiHttpHandler : HttpMessageHandler
    {
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

        public bool AllowAutoRedirect
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }
#pragma warning restore CA1822
        #endregion

        internal ClientCertificateOption ClientCertificateOptions;

        public const bool SupportsAutomaticDecompression = false;
        public const bool SupportsProxy = false;
        public const bool SupportsRedirectConfiguration = false;

        private Dictionary<string, object?>? _properties;
        public IDictionary<string, object?> Properties =>
            _properties ??= new Dictionary<string, object?>();

        protected internal override HttpResponseMessage Send(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            throw new PlatformNotSupportedException();
        }

        protected internal override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (request.RequestUri is null)
            {
                throw new ArgumentException();
            }

            var requestMethod = request.Method.ToString();
            var uri = request.RequestUri;

            ITypes.Method method;
            switch (requestMethod)
            {
                case "":
                case "GET":
                    method = ITypes.Method.get();
                    break;
                case "HEAD":
                    method = ITypes.Method.head();
                    break;
                case "POST":
                    method = ITypes.Method.post();
                    break;
                case "PUT":
                    method = ITypes.Method.put();
                    break;
                case "DELETE":
                    method = ITypes.Method.delete();
                    break;
                case "CONNECT":
                    method = ITypes.Method.connect();
                    break;
                case "OPTIONS":
                    method = ITypes.Method.options();
                    break;
                case "TRACE":
                    method = ITypes.Method.trace();
                    break;
                case "PATCH":
                    method = ITypes.Method.patch();
                    break;
                default:
                    method = ITypes.Method.other(requestMethod);
                    break;
            }

            ITypes.Scheme scheme;
            switch (uri.Scheme)
            {
                case "":
                case "http":
                    scheme = ITypes.Scheme.http();
                    break;
                case "https":
                    scheme = ITypes.Scheme.https();
                    break;
                default:
                    scheme = ITypes.Scheme.other(uri.Scheme);
                    break;
            }

            string authority;
            if (uri.Authority.Length == 0)
            {
                // `wasi:http/outgoing-handler` requires a non-empty authority,
                // so we set one here:
                if (scheme.Tag == ITypes.Scheme.HTTPS)
                {
                    authority = ":443";
                }
                else
                {
                    authority = ":80";
                }
            }
            else
            {
                authority = uri.Authority;
            }

            var headers = new List<(string, byte[])>();
            foreach (var pair in request.Headers)
            {
                foreach (var value in pair.Value)
                {
                    headers.Add((pair.Key, Encoding.UTF8.GetBytes(value)));
                }
            }
            if (request.Content is not null)
            {
                foreach (var pair in request.Content.Headers)
                {
                    foreach (var value in pair.Value)
                    {
                        headers.Add((pair.Key, Encoding.UTF8.GetBytes(value)));
                    }
                }
            }

            var outgoingRequest = new ITypes.OutgoingRequest(ITypes.Fields.FromList(headers));
            outgoingRequest.SetMethod(method);
            outgoingRequest.SetScheme(scheme);
            outgoingRequest.SetAuthority(authority);
            outgoingRequest.SetPathWithQuery(uri.PathAndQuery);

            var outgoingStream = new OutputStream(outgoingRequest.Body());

            Func<Task<ITypes.IncomingResponse?>> sendContent = async () =>
            {
                await SendContentAsync(request.Content, outgoingStream).ConfigureAwait(false);
                return null;
            };

            // Concurrently send the request and the content stream, allowing
            // the server to start sending a response before it's received the
            // entire request body.
            var incomingResponse = (
                await Task.WhenAll(
                        new Task<ITypes.IncomingResponse?>[]
                        {
                            SendRequestAsync(outgoingRequest),
                            sendContent()
                        }
                    )
                    .ConfigureAwait(false)
            )[0];

            if (incomingResponse is null)
            {
                // Shouldn't be possible, since `SendRequestAsync` always
                // returns a non-null value.
                throw new Exception("unreachable code");
            }

            var response = new HttpResponseMessage((HttpStatusCode)incomingResponse.Status());
            var responseHeaders = incomingResponse.Headers().Entries();
            response.Content = new StreamContent(new InputStream(incomingResponse.Consume()));
            foreach ((var key, var value) in responseHeaders)
            {
                var valueString = Encoding.UTF8.GetString(value);
                if (
                    HeaderDescriptor.TryGet(key, out HeaderDescriptor descriptor)
                    && (descriptor.HeaderType & HttpHeaderType.Content) != 0
                )
                {
                    response.Content.Headers.Add(key, valueString);
                }
                else
                {
                    response.Headers.Add(key, valueString);
                }
            }

            return response;
        }

        private static async Task<ITypes.IncomingResponse?> SendRequestAsync(
            ITypes.OutgoingRequest request
        )
        {
            ITypes.FutureIncomingResponse future;
            try
            {
                future = OutgoingHandlerInterop.Handle(request, null);
            }
            catch (WasiHttpWorld.WitException e)
            {
                var message = ErrorCodeToString((ITypes.ErrorCode)e.Value);
                throw new Exception($"Request Error: {message}");
            }

            while (true)
            {
                var response = future.Get();
                if (response is not null)
                {
                    var result = (
                        (Result<Result<ITypes.IncomingResponse, ITypes.ErrorCode>, None>)response
                    ).AsOk;

                    if (result.IsOk)
                    {
                        return result.AsOk;
                    }
                    else
                    {
                        var message = ErrorCodeToString(result.AsErr);
                        throw new Exception($"Request Error: {message}");
                    }
                }
                else
                {
                    await WasiEventLoop.RegisterWasiPollable(future.Subscribe()).ConfigureAwait(false);
                }
            }
        }

        private static string ErrorCodeToString(ITypes.ErrorCode code)
        {
            // TODO: include payload data in result where applicable
            switch (code.Tag)
            {
                case ITypes.ErrorCode.DNS_TIMEOUT:
                    return "DNS_TIMEOUT";

                case ITypes.ErrorCode.DNS_ERROR:
                    return "DNS_ERROR";

                case ITypes.ErrorCode.DESTINATION_NOT_FOUND:
                    return "DESTINATION_NOT_FOUND";

                case ITypes.ErrorCode.DESTINATION_UNAVAILABLE:
                    return "DESTINATION_UNAVAILABLE";

                case ITypes.ErrorCode.DESTINATION_IP_PROHIBITED:
                    return "DESTINATION_IP_PROHIBITED";

                case ITypes.ErrorCode.DESTINATION_IP_UNROUTABLE:
                    return "DESTINATION_IP_UNROUTABLE";

                case ITypes.ErrorCode.CONNECTION_REFUSED:
                    return "CONNECTION_REFUSED";

                case ITypes.ErrorCode.CONNECTION_TERMINATED:
                    return "CONNECTION_TERMINATED";

                case ITypes.ErrorCode.CONNECTION_TIMEOUT:
                    return "CONNECTION_TIMEOUT";

                case ITypes.ErrorCode.CONNECTION_READ_TIMEOUT:
                    return "CONNECTION_READ_TIMEOUT";

                case ITypes.ErrorCode.CONNECTION_WRITE_TIMEOUT:
                    return "CONNECTION_WRITE_TIMEOUT";

                case ITypes.ErrorCode.CONNECTION_LIMIT_REACHED:
                    return "CONNECTION_LIMIT_REACHED";

                case ITypes.ErrorCode.TLS_PROTOCOL_ERROR:
                    return "TLS_PROTOCOL_ERROR";

                case ITypes.ErrorCode.TLS_CERTIFICATE_ERROR:
                    return "TLS_CERTIFICATE_ERROR";

                case ITypes.ErrorCode.TLS_ALERT_RECEIVED:
                    return "TLS_ALERT_RECEIVED";

                case ITypes.ErrorCode.HTTP_REQUEST_DENIED:
                    return "HTTP_REQUEST_DENIED";

                case ITypes.ErrorCode.HTTP_REQUEST_LENGTH_REQUIRED:
                    return "HTTP_REQUEST_LENGTH_REQUIRED";

                case ITypes.ErrorCode.HTTP_REQUEST_BODY_SIZE:
                    return "HTTP_REQUEST_BODY_SIZE";

                case ITypes.ErrorCode.HTTP_REQUEST_METHOD_INVALID:
                    return "HTTP_REQUEST_METHOD_INVALID";

                case ITypes.ErrorCode.HTTP_REQUEST_URI_INVALID:
                    return "HTTP_REQUEST_URI_INVALID";

                case ITypes.ErrorCode.HTTP_REQUEST_URI_TOO_LONG:
                    return "HTTP_REQUEST_URI_TOO_LONG";

                case ITypes.ErrorCode.HTTP_REQUEST_HEADER_SECTION_SIZE:
                    return "HTTP_REQUEST_HEADER_SECTION_SIZE";

                case ITypes.ErrorCode.HTTP_REQUEST_HEADER_SIZE:
                    return "HTTP_REQUEST_HEADER_SIZE";

                case ITypes.ErrorCode.HTTP_REQUEST_TRAILER_SECTION_SIZE:
                    return "HTTP_REQUEST_TRAILER_SECTION_SIZE";

                case ITypes.ErrorCode.HTTP_REQUEST_TRAILER_SIZE:
                    return "HTTP_REQUEST_TRAILER_SIZE";

                case ITypes.ErrorCode.HTTP_RESPONSE_INCOMPLETE:
                    return "HTTP_RESPONSE_INCOMPLETE";

                case ITypes.ErrorCode.HTTP_RESPONSE_HEADER_SECTION_SIZE:
                    return "HTTP_RESPONSE_HEADER_SECTION_SIZE";

                case ITypes.ErrorCode.HTTP_RESPONSE_HEADER_SIZE:
                    return "HTTP_RESPONSE_HEADER_SIZE";

                case ITypes.ErrorCode.HTTP_RESPONSE_BODY_SIZE:
                    return "HTTP_RESPONSE_BODY_SIZE";

                case ITypes.ErrorCode.HTTP_RESPONSE_TRAILER_SECTION_SIZE:
                    return "HTTP_RESPONSE_TRAILER_SECTION_SIZE";

                case ITypes.ErrorCode.HTTP_RESPONSE_TRAILER_SIZE:
                    return "HTTP_RESPONSE_TRAILER_SIZE";

                case ITypes.ErrorCode.HTTP_RESPONSE_TRANSFER_CODING:
                    return "HTTP_RESPONSE_TRANSFER_CODING";

                case ITypes.ErrorCode.HTTP_RESPONSE_CONTENT_CODING:
                    return "HTTP_RESPONSE_CONTENT_CODING";

                case ITypes.ErrorCode.HTTP_RESPONSE_TIMEOUT:
                    return "HTTP_RESPONSE_TIMEOUT";

                case ITypes.ErrorCode.HTTP_UPGRADE_FAILED:
                    return "HTTP_UPGRADE_FAILED";

                case ITypes.ErrorCode.HTTP_PROTOCOL_ERROR:
                    return "HTTP_PROTOCOL_ERROR";

                case ITypes.ErrorCode.LOOP_DETECTED:
                    return "LOOP_DETECTED";

                case ITypes.ErrorCode.CONFIGURATION_ERROR:
                    return "CONFIGURATION_ERROR";

                case ITypes.ErrorCode.INTERNAL_ERROR:
                    return "INTERNAL_ERROR";

                default:
                    return $"{code.Tag}";
            }
        }

        private static async Task SendContentAsync(HttpContent? content, Stream stream)
        {
            try
            {
                if (content is not null)
                {
                    await content.CopyToAsync(stream).ConfigureAwait(false);
                }
            }
            finally
            {
                stream.Dispose();
            }
        }

        private static class WasiEventLoop
        {
            internal static Task RegisterWasiPollable(IPoll.Pollable pollable)
            {
                var handle = pollable.Handle;
                pollable.Handle = 0;
                return CallRegisterWasiPollable((Thread)null!, handle);

                [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "RegisterWasiPollable")]
                static extern Task CallRegisterWasiPollable(Thread t, int handle);
            }
        }

        private sealed class InputStream : Stream
        {
            private ITypes.IncomingBody body;
            private IStreams.InputStream stream;
            private int offset;
            private byte[]? buffer;
            private bool closed;

            public InputStream(ITypes.IncomingBody body)
            {
                this.body = body;
                this.stream = body.Stream();
            }

            ~InputStream()
            {
                Dispose(false);
            }

            public override bool CanRead => true;
            public override bool CanWrite => false;
            public override bool CanSeek => false;
            public override long Length => throw new NotImplementedException();
            public override long Position
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            protected override void Dispose(bool disposing)
            {
                stream.Dispose();
                ITypes.IncomingBody.Finish(body);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void Flush()
            {
                // ignore
            }

            public override void SetLength(long length)
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int length)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int length)
            {
                throw new NotImplementedException();
            }

            public override async Task<int> ReadAsync(
                byte[] bytes,
                int offset,
                int length,
                CancellationToken cancellationToken
            )
            {
                // TODO: handle `cancellationToken`
                while (true)
                {
                    if (closed)
                    {
                        return 0;
                    }
                    else if (this.buffer == null)
                    {
                        try
                        {
                            // TODO: should we add a special case to the bindings generator
                            // to allow passing a buffer to IStreams.InputStream.Read and
                            // avoid the extra copy?
                            var result = stream.Read(16 * 1024);
                            var buffer = result;
                            if (buffer.Length == 0)
                            {
                                await WasiEventLoop
                                    .RegisterWasiPollable(stream.Subscribe())
                                    .ConfigureAwait(false);
                            }
                            else
                            {
                                this.buffer = buffer;
                                this.offset = 0;
                            }
                        }
                        catch (WitException e)
                        {
                            if (((IStreams.StreamError)e.Value).Tag == IStreams.StreamError.CLOSED)
                            {
                                closed = true;
                                return 0;
                            }
                            else
                            {
                                throw e;
                            }
                        }
                    }
                    else
                    {
                        var min = Math.Min(this.buffer.Length - this.offset, length);
                        Array.Copy(this.buffer, this.offset, bytes, offset, min);
                        if (min < buffer.Length - this.offset)
                        {
                            this.offset += min;
                        }
                        else
                        {
                            this.buffer = null;
                        }
                        return min;
                    }
                }
            }

            public override async ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default
            )
            {
                // TODO: avoid copy when possible and use ArrayPool when not
                var dst = new byte[buffer.Length];
                // We disable "CA1835: Prefer the memory-based overloads of
                // ReadAsync/WriteAsync methods in stream-based classes" for
                // now, since `ReadyAsync(byte[], int, int, CancellationToken)`
                // is where the implementation currently resides, but we should
                // revisit this if/when `wit-bindgen` learns to generate
                // memory-based bindings.
#pragma warning disable CA1835
                var result = await ReadAsync(dst, 0, buffer.Length, cancellationToken)
                    .ConfigureAwait(false);
#pragma warning restore CA1835
                new ReadOnlySpan<byte>(dst, 0, result).CopyTo(buffer.Span);
                return result;
            }
        }

        private sealed class OutputStream : Stream
        {
            private ITypes.OutgoingBody body;
            private IStreams.OutputStream stream;

            public OutputStream(ITypes.OutgoingBody body)
            {
                this.body = body;
                this.stream = body.Write();
            }

            ~OutputStream()
            {
                Dispose(false);
            }

            public override bool CanRead => false;
            public override bool CanWrite => true;
            public override bool CanSeek => false;
            public override long Length => throw new NotImplementedException();
            public override long Position
            {
                get => throw new NotImplementedException();
                set => throw new NotImplementedException();
            }

            protected override void Dispose(bool disposing)
            {
                stream.Dispose();
                ITypes.OutgoingBody.Finish(body, null);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void Flush()
            {
                // ignore
                //
                // Note that flushing a `wasi:io/streams/output-stream` is an
                // asynchronous operation, so it's not clear how we would
                // implement it here instead of taking care of it as part of
                // `WriteAsync`.
            }

            public override void SetLength(long length)
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int length)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int length)
            {
                throw new NotImplementedException();
            }

            public override async Task WriteAsync(
                byte[] bytes,
                int offset,
                int length,
                CancellationToken cancellationToken
            )
            {
                var limit = offset + length;
                var flushing = false;
                while (true)
                {
                    var count = (int)stream.CheckWrite();
                    if (count == 0)
                    {
                        await WasiEventLoop.RegisterWasiPollable(stream.Subscribe()).ConfigureAwait(false);
                    }
                    else if (offset == limit)
                    {
                        if (flushing)
                        {
                            return;
                        }
                        else
                        {
                            stream.Flush();
                            flushing = true;
                        }
                    }
                    else
                    {
                        var min = Math.Min(count, limit - offset);
                        if (offset == 0 && min == bytes.Length)
                        {
                            stream.Write(bytes);
                        }
                        else
                        {
                            // TODO: is there a more efficient option than copying here?
                            // Do we need to change the binding generator to accept
                            // e.g. `Span`s?
                            var copy = new byte[min];
                            Array.Copy(bytes, offset, copy, 0, min);
                            stream.Write(copy);
                        }
                        offset += min;
                    }
                }
            }

            public override ValueTask WriteAsync(
                ReadOnlyMemory<byte> buffer,
                CancellationToken cancellationToken = default
            )
            {
                // TODO: avoid copy when possible and use ArrayPool when not
                var copy = new byte[buffer.Length];
                buffer.Span.CopyTo(copy);
                return new ValueTask(WriteAsync(copy, 0, buffer.Length, cancellationToken));
            }
        }
    }
}
