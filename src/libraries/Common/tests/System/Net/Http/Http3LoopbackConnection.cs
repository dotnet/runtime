// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public sealed class Http3LoopbackConnection : GenericLoopbackConnection
    {
        private readonly QuicConnection _connection;
        private readonly Dictionary<int, Http3LoopbackStream> _openStreams = new Dictionary<int, Http3LoopbackStream>();
        private Http3LoopbackStream _currentStream;

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

            _connection.Dispose();
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
            Http3LoopbackStream stream = await AcceptStreamAsync().ConfigureAwait(false);
            return await stream.ReadRequestDataAsync(readBody).ConfigureAwait(false);
        }

        public override async Task SendResponseAsync(HttpStatusCode? statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "", bool isFinal = true, int requestId = 0)
        {
            await SendResponseHeadersAsync(statusCode, headers, requestId).ConfigureAwait(false);
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
                stream.ShutdownSend();
                stream.Dispose();
            }
        }

        public override Task SendResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, int requestId = 0)
        {
            return SendResponseHeadersAsync(statusCode, headers, requestId);
        }

        private async Task SendResponseHeadersAsync(HttpStatusCode? statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, int requestId = 0)
        {
            var allHeaders = new List<HttpHeaderData>((headers?.Count ?? 0) + 1);

            if (statusCode != null)
            {
                allHeaders.Add(new HttpHeaderData(":status", ((int)statusCode).ToString(CultureInfo.InvariantCulture)));
            }

            allHeaders.AddRange(headers);

            await GetOpenRequest(requestId).SendHeadersFrameAsync(allHeaders).ConfigureAwait(false);
        }

        public override async Task WaitForCancellationAsync(bool ignoreIncomingData = true, int requestId = 0)
        {
            await GetOpenRequest(requestId).WaitForCancellationAsync(ignoreIncomingData).ConfigureAwait(false);
        }
    }

}
