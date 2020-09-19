// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Net.Http
{
    /// <summary>
    /// Represents the context passed to the PlaintextStreamFilter for a SocketsHttpHandler instance.
    /// </summary>
    public sealed class SocketsHttpPlaintextStreamFilterContext
    {
        private readonly Stream _plaintextStream;
        private readonly Version _httpVersion;
        private readonly HttpRequestMessage _requestMessage;

        internal SocketsHttpPlaintextStreamFilterContext(Stream plaintextStream, Version httpVersion, HttpRequestMessage requestMessage)
        {
            _plaintextStream = plaintextStream;
            _httpVersion = httpVersion;
            _requestMessage = requestMessage;
        }

        /// <summary>
        /// The plaintext Stream that will be used for HTTP protocol requests and responses.
        /// </summary>
        public Stream PlaintextStream => _plaintextStream;

        /// <summary>
        /// The version of HTTP in use for this stream.
        /// </summary>
        public Version HttpVersion => _httpVersion;

        /// <summary>
        /// The initial HttpRequestMessage that is causing the stream to be used.
        /// </summary>
        public HttpRequestMessage RequestMessage => _requestMessage;
    }
}
