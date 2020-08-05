// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Net.Http.Functional.Tests;

namespace System.Net.Test.Common
{
    internal sealed class Http3LoopbackConnection : GenericLoopbackConnection
    {
        public const long H3_NO_ERROR = 0x100;
        public const long H3_GENERAL_PROTOCOL_ERROR = 0x101;
        public const long H3_INTERNAL_ERROR = 0x102;
        public const long H3_STREAM_CREATION_ERROR = 0x103;
        public const long H3_CLOSED_CRITICAL_STREAM = 0x104;
        public const long H3_FRAME_UNEXPECTED = 0x105;
        public const long H3_FRAME_ERROR = 0x106;
        public const long H3_EXCESSIVE_LOAD = 0x107;
        public const long H3_ID_ERROR = 0x108;
        public const long H3_SETTINGS_ERROR = 0x109;
        public const long H3_MISSING_SETTINGS = 0x10a;
        public const long H3_REQUEST_REJECTED = 0x10b;
        public const long H3_REQUEST_CANCELLED = 0x10c;
        public const long H3_REQUEST_INCOMPLETE = 0x10d;
        public const long H3_CONNECT_ERROR = 0x10f;
        public const long H3_VERSION_FALLBACK = 0x110;

        private readonly QuicConnection _connection;
        private readonly Dictionary<int, Http3LoopbackStream> _openStreams = new Dictionary<int, Http3LoopbackStream>();
        private Http3LoopbackStream _currentStream;
        private bool _closed;

        public Http3LoopbackConnection(QuicConnection connection)
        {
            _connection = connection;
        }

        public override void Dispose()
        {
            foreach (Http3LoopbackStream stream in _openStreams.Values)
            {
                stream.Dispose();
            }

            if (!_closed)
            {
            //    CloseAsync(H3_INTERNAL_ERROR).GetAwaiter().GetResult();
            }

            //_connection.Dispose();
        }

        public async Task CloseAsync(long errorCode)
        {
            await _connection.CloseAsync(errorCode).ConfigureAwait(false);
            _closed = true;
        }

        public Http3LoopbackStream OpenUnidirectionalStream()
        {
            return new Http3LoopbackStream(_connection.OpenUnidirectionalStream());
        }

        public Http3LoopbackStream OpenBidirectionalStream()
        {
            return new Http3LoopbackStream(_connection.OpenBidirectionalStream());
        }

        public static int GetRequestId(QuicStream stream)
        {
            Debug.Assert(stream.CanRead && stream.CanWrite, "Stream must be a request stream.");

            // TODO: QUIC streams can have IDs larger than int.MaxValue; update all our tests to use long rather than int.
            return checked((int)stream.StreamId + 1);
        }

        public Http3LoopbackStream GetOpenRequest(int requestId = 0)
        {
            return requestId == 0 ? _currentStream : _openStreams[requestId - 1];
        }

        public override Task InitializeConnectionAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<Http3LoopbackStream> AcceptStreamAsync()
        {
            QuicStream quicStream = await _connection.AcceptStreamAsync().ConfigureAwait(false);
            var stream = new Http3LoopbackStream(quicStream);

            _openStreams.Add(checked((int)quicStream.StreamId), stream);
            _currentStream = stream;

            return stream;
        }

        public override async Task<byte[]> ReadRequestBodyAsync()
        {
            return await _currentStream.ReadRequestBodyAsync().ConfigureAwait(false);
        }

        public override async Task<HttpRequestData> ReadRequestDataAsync(bool readBody = true)
        {
            Http3LoopbackStream stream;

            do
            {
                stream = await AcceptStreamAsync().ConfigureAwait(false);
            }
            while (!stream.CanWrite); // skip control stream.

            return await stream.ReadRequestDataAsync(readBody).ConfigureAwait(false);
        }

        public override async Task SendResponseAsync(HttpStatusCode? statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "", bool isFinal = true, int requestId = 0)
        {
            IEnumerable<HttpHeaderData> newHeaders = headers ?? Enumerable.Empty<HttpHeaderData>();

            if (content != null && !newHeaders.Any(x => x.Name == "Content-Length"))
            {
                newHeaders = newHeaders.Append(new HttpHeaderData("Content-Length", content.Length.ToString(CultureInfo.InvariantCulture)));
            }

            await SendResponseHeadersAsync(statusCode, newHeaders, requestId).ConfigureAwait(false);
            await SendResponseBodyAsync(Encoding.UTF8.GetBytes(content ?? ""), isFinal, requestId).ConfigureAwait(false);
        }

        public override async Task SendResponseBodyAsync(byte[] content, bool isFinal = true, int requestId = 0)
        {
            Http3LoopbackStream stream = GetOpenRequest(requestId);

            if (content?.Length != 0)
            {
                await stream.SendDataFrameAsync(content).ConfigureAwait(false);
            }

            if (isFinal)
            {
                await stream.ShutdownSendAsync().ConfigureAwait(false);
                stream.Dispose();
            }
        }

        public override Task SendResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, int requestId = 0)
        {
            return SendResponseHeadersAsync(statusCode, headers, requestId);
        }

        private async Task SendResponseHeadersAsync(HttpStatusCode? statusCode = HttpStatusCode.OK, IEnumerable<HttpHeaderData> headers = null, int requestId = 0)
        {
            headers ??= Enumerable.Empty<HttpHeaderData>();

            // Some tests use Content-Length with a null value to indicate Content-Length should not be set.
            headers = headers.Where(x => x.Name != "Content-Length" || x.Value != null);

            if (statusCode != null)
            {
                headers = headers.Prepend(new HttpHeaderData(":status", ((int)statusCode).ToString(CultureInfo.InvariantCulture)));
            }

            await GetOpenRequest(requestId).SendHeadersFrameAsync(headers).ConfigureAwait(false);
        }

        public override async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            HttpRequestData request = await ReadRequestDataAsync().ConfigureAwait(false);
            await SendResponseAsync(statusCode, headers, content).ConfigureAwait(false);
            await CloseAsync(Http3LoopbackConnection.H3_NO_ERROR);
            return request;
        }

        public override async Task WaitForCancellationAsync(bool ignoreIncomingData = true, int requestId = 0)
        {
            await GetOpenRequest(requestId).WaitForCancellationAsync(ignoreIncomingData).ConfigureAwait(false);
        }
    }

}
